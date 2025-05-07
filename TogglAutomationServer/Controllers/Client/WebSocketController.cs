using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TogglAutomationServer.Models;
using TogglAutomationServer.Toggl;
using UskokWS;

namespace TogglAutomationServer.Controllers.Client;

[Route("agent")]
public class WebSocketController : WebSocketController<AgentClient>
{
    private readonly ILogger<WebSocketController> _logger;
    private readonly TogglSettings _settings;
    public WebSocketController(ILogger<WebSocketController> logger, TogglSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    private SessionData SessionData { get; set; } = null!;
    [HttpGet("{sessionId:guid}/{version}")]
    public async Task Init(Guid sessionId, string version, CancellationToken token)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Response.StatusCode = 400;
            return;
        }

        if (!SessionController.Sessions.TryGetValue(sessionId, out var sessionData))
        {
            Response.StatusCode = 404;
            return;
        }

        if (version != VersionControl.VERSION && version != "debug")
        {
            Response.StatusCode = 401;
            return;
        }

        SessionData = sessionData;
        await HandleConnection(HttpContext, token);
    }

    public override async Task OnDisconnected(WebSocketCloseStatus status)
    {
        await Log.InsertLog(new Log(Client.SessionData.Extension, LogType.Disconnected));
        _logger.LogWarning("Agent {Extension} disconnected status: {Status}", SessionData.Extension, status);
        await base.OnDisconnected(status);
    }

    private static Task<CurrentTimeEndpoint?> GetCurrentTime(AgentClient client, CancellationToken token) =>
        client.SessionData.HttpClient.GetFromJsonAsync<CurrentTimeEndpoint>("https://api.track.toggl.com/api/v9/me/time_entries/current", token);

    private record CurrentTimeEndpoint([property: JsonPropertyName("project_id")] int? ProjectId, [property: JsonPropertyName("id")] long Id);
    public override async Task OnConnected(CancellationToken token)
    {
        Client.SessionData = SessionData;
        bool isInProject = false;
        try
        {
            if (Client.SessionData.BeforeCallProject != null) return;
            var response = await GetCurrentTime(Client, token);
            if (response?.ProjectId == null)
            {
                _logger.LogInformation("Agent {Extension} connected, no projectId", SessionData.Extension);
            }
            else
            {
                //Client.SessionData.LastProjectId = response.ProjectId.Value;
                _logger.LogInformation("Agent {Extension} connected, current projectId: {ProjectId}", SessionData.Extension, Client.SessionData.BeforeCallProject?.ProjectId);
                isInProject = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed getting current task for {Extension} {UserId}, error text: {Error}", SessionData.Extension, SessionData.UserId, ex.Message);
        }
        if (Client.SessionData.InCall)
        {
            await Client.Light("red", false);
        }
        else
        {
            await Client.Light(isInProject ? "blue" : "green", false);
        }
        await new Log(Client.SessionData.Extension, LogType.Connected, Client.SessionData.BeforeCallProject).Insert();
    }

    public override async Task OnMessage(MemoryStream stream, WebSocketMessageType type)
    {
        if (type != WebSocketMessageType.Text) return;

        try
        {
            var jsonValue = await JsonSerializer.DeserializeAsync<JsonObject>(stream, cancellationToken: Client.ClientCancellationToken);
            if (jsonValue == null)
            {
                _logger.LogError("Extension({Extension}) received null object", SessionData.Extension);
                return;
            }
            
            var messageType = jsonValue["type"]?.GetValue<string>();
            if (messageType != "select") return;

            var data = jsonValue["data"];
            if (data == null) return;

            var projectId = data["projectId"]?.GetValue<int>();
            var option = data["option"]?.GetValue<int>();
            if (option == null) return;

            LogType logType = LogType.DoNothing;
            if (option == 0) logType = LogType.WorkBeforeCall;
            else if (option == 1) logType = LogType.WorkAfterCall;

            //await Log.InsertLog(new Log(Client.SessionData.Extension, logType, projectId));
            if (projectId == null || option == 2)
            {
                return;
            }

            string desc = option == 0 ? "Agent returned to work before the call" : "Agent continued to work for the call";
            _logger.LogInformation("Starting timer Id: {Id}", projectId);

            try
            {
                var currentTime = await GetCurrentTime(Client, Client.ClientCancellationToken);
                if (currentTime != null)
                {
                    await Client.SessionData.HttpClient.PatchAsync($"https://api.track.toggl.com/api/v9/workspaces/{_settings.WorkspaceId}/time_entries/{currentTime.Id}/stop", null);
                    _logger.LogInformation("Stopping current time entry {Id}", currentTime.Id);
                }

                var postData = new
                {
                    billable = true,
                    created_with = "Windows APP",
                    description = desc,
                    project_id = projectId,
                    workspace_id = int.Parse(_settings.WorkspaceId),
                    duration = -1,
                    start = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                var response = await SessionData.HttpClient.PostAsJsonAsync(
                    $"https://api.track.toggl.com/api/v9/workspaces/{_settings.WorkspaceId}/time_entries",
                    postData);
                _logger.LogInformation("Timer response status code: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error with {Extension}, error: {Message}", SessionData.Extension, ex.Message);
                await Client.SendTyped("msgBox", "Failed to start the timer");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error on extension({Extension}), Error: {Message}", SessionData.Extension, ex.Message);
        }
    }
}

public class AgentClient : WebSocketClient
{
    public SessionData SessionData { get; set; } = null!;

    public Task SendTyped(string type, object? data)
    {
        var jsonStr = JsonSerializer.Serialize(new
        {
            type,
            data
        });
        var bytes = Encoding.UTF8.GetBytes(jsonStr);

        return Socket.SendAsync(bytes, WebSocketMessageType.Text, true, ClientCancellationToken);
    }

    public Task Light(string color, bool pulse)
    {
        return SendTyped("light", new
        {
            color,
            pulse
        });
    }
}
