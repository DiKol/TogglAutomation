using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using TogglAutomationServer.Toggl;

namespace TogglAutomationServer.Controllers.Client;

[ApiController]
[Route("session")]
public class SessionController : ControllerBase
{
    public static readonly ConcurrentDictionary<Guid, SessionData> Sessions = new();
    private readonly ILogger<SessionController> _logger;
    public SessionController(ILogger<SessionController> logger)
    {
        _logger = logger;
    }
    public record SessionCreateData(string Email, string Password, string Extension);

    private record MeEndpointData(int Id);
    [HttpPut("create")]
    public async Task<IActionResult> CreateSession([FromBody] SessionCreateData createData, CancellationToken token)
    {
        var alreadyEmailSessions = Sessions.Where(x => x.Value.Email == createData.Email);
        foreach (var session in alreadyEmailSessions)
        {
            Sessions.Remove(session.Key, out _);
            session.Value.HttpClient.Dispose();
        }
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{createData.Email}:{createData.Password}")));

        int userId;
        try
        {
            var response = await httpClient.GetFromJsonAsync<MeEndpointData>("https://api.track.toggl.com/api/v9/me", token);
            if (response == null)
            {
                return BadRequest("Error reading data from toggl");
            }
            userId = response.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(message: ex.ToString());
            return BadRequest("Wrong email/password");
        }

        var sessionId = Guid.NewGuid();
        Sessions[sessionId] = new SessionData
        {
            Email = createData.Email,
            HttpClient = httpClient,
            UserId = userId,
            Extension = createData.Extension
        };
        _logger.LogInformation("New session Id:{SessionId}, UserID: {UserId}, Ext:{Extension}, Email:{Email}", sessionId, userId, createData.Extension, createData.Email);
        return Ok(new
        {
            sessionId
        });
    }
}

public class SessionData
{
    public HttpClient HttpClient { get; init; } = null!;
    public bool InCall { get; set; } = false;
    public int UserId { get; init; }
    public string Email { get; init; } = null!;
    public string Extension { get; init; } = null!;
    public Project? BeforeCallProject { get; set; }
    public Project? CallProject { get; set; }
}