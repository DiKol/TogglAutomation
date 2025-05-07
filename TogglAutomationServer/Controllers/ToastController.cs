using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TogglAutomationServer.Controllers.Client;

namespace TogglAutomationServer.Controllers
{
    [Route("toast")]
    [ApiController]
    public class ToastController : ControllerBase
    {
        public class ToastElement
        {
            public string Type { get; set; } = null!;
            public string? Url { get; set; }
            public string? Text { get; set; }
        }

        public record ToastBody(string Extension, ToastElement[] Elements);
        [HttpPost]
        public async Task<IActionResult> SendToast([FromBody]ToastBody body)
        {
            var connectedWebsocket = WebSocketController.ConnectedSockets.Values.FirstOrDefault(x => x.SessionData.Extension == body.Extension);
            if (connectedWebsocket == null)
            {
                return NotFound("Agent not found using the app");
            }

            await connectedWebsocket.SendTyped("toast", body.Elements);
            return Ok();
        }
    }
}
