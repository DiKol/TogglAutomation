using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MySqlConnector;
using System.Text.Json.Serialization;
using TogglAutomationServer.Controllers.Client;
using TogglAutomationServer.Models;
using TogglAutomationServer.Toggl;

namespace TogglAutomationServer.Controllers;

[ApiController]
[Route("")]

public class IgorController : ControllerBase
{
    private readonly ILogger<IgorController> _logger;
    private readonly TogglSettings _settings;
    public IgorController(ILogger<IgorController> logger, TogglSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    [HttpPost("callStarted/{extension}")]
    [IgorFilter(LogType = LogType.CallStarted)]
    public async Task<IActionResult> CallStarted(string extension)
    {
        AgentClient agent = GetContextAgent();

        _logger.LogInformation("Agent {Extension} call", extension);

        agent.SessionData.InCall = true;
        await agent.Light("red", false);
        return Ok("Agent notified about call started");
    }

    [HttpPost("callEnded/{projectId:int}/{extension}")]
    [IgorFilter(LogType = LogType.CallEnded)]
    public async Task<IActionResult> CallEnded(int projectId, string extension)
    {
        AgentClient agent = GetContextAgent();
        _logger.LogInformation("Agent {Extension} call ended project: {ProjectId}", extension, projectId);
        agent.SessionData.CallProject = await Project.GetProjectAsync(agent.SessionData.HttpClient, projectId, _settings.WorkspaceId);
        agent.SessionData.InCall = false;
        await agent.Light("green", false);
        await agent.SendTyped("select", new
        {
            inCall = agent.SessionData.CallProject,
            beforeCall = agent.SessionData.BeforeCallProject
        });
        return Ok("Agent notified about call ended");
    }

    private static readonly Project _unkownProject = new()
    {
        Color = "#515148",
        Name = "Unknown",
        ProjectId = 0
    };

    private record CurrentProjectApiCall([property:JsonPropertyName("project_id")]long ProjectId);

    [HttpPost("callIncoming/{extension}")]
    [IgorFilter(LogType = LogType.CallIncoming)]
    public async Task<IActionResult> CallIncoming(string extension)
    {
        AgentClient agent = GetContextAgent();
        if (!agent.SessionData.InCall)
        {
            var currentProject = await agent.SessionData.HttpClient.GetFromJsonAsync<CurrentProjectApiCall>("https://api.track.toggl.com/api/v9/me/time_entries/current");
            agent.SessionData.BeforeCallProject = currentProject == null ?
                null :
                await Project.GetProjectAsync(agent.SessionData.HttpClient, currentProject.ProjectId, _settings.WorkspaceId);
        }

        _logger.LogInformation("Agent {Extension} call incoming", extension);
        await agent.Light(agent.SessionData.InCall ? "red" : "yellow", true);
        return Ok("Agent notified about the incoming call");
    }

    
    [HttpPost("callMissedOrDeclined/{extension}")]
    [IgorFilter(LogType = LogType.CallMissedOrDeclined)]
    public async Task<IActionResult> CallMissedOrDeicled(string extension)
    {
        AgentClient agent = GetContextAgent();

        _logger.LogInformation("Agent {Extension} call missed/declined", extension);

        var sessionData = agent.SessionData;
        if (sessionData.InCall)
        {
            await agent.Light("red", false);
        }
        else if (sessionData.BeforeCallProject != null)
        {
            await agent.Light("blue", false);
        }
        else
        {
            await agent.Light("green", false);
        }
        return Ok("Agent notified about the declined call");
    }

    private AgentClient GetContextAgent() => (HttpContext.Items["agent"] as AgentClient)!;
}


public class IgorFilterAttribute : ActionFilterAttribute
{
    public LogType LogType { get; set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var extension = context.HttpContext.Request.RouteValues["extension"]?.ToString();
        if (string.IsNullOrEmpty(extension))
        {
            context.Result = new BadRequestObjectResult("Extension not provided");
            return;
        }
        Log log = new(extension, LogType);

        try
        {
            var connectedWebsocket = WebSocketController.ConnectedSockets.Values.FirstOrDefault(x => x.SessionData.Extension == extension);
            if (connectedWebsocket == null)
            {
                log.UsingTheApp = false;
                context.Result = new OkObjectResult("Agent not using the app");
                return;
            }
            if(LogType == LogType.CallIncoming)
            {
                log.Project = connectedWebsocket.SessionData.BeforeCallProject;
            }
            else if(LogType == LogType.CallEnded)
            {
                //todo callended proejct
            }
            context.HttpContext.Items["agent"] = connectedWebsocket;
            await next();
        }
        finally
        {
            await log.Insert();
        }
    }
}