using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TogglAutomationServer.Controllers.Client
{
    [Route("version")]
    [ApiController]
    public class VersionControl : ControllerBase
    {
        public const string VERSION = "v6";

        [HttpGet]
        public IActionResult GetCurrentVersion()
        {
            return Ok(new
            {
                version = VERSION
            });
        }

        [HttpGet("download")]
        public IActionResult DownloadFile()
        {
            return File("TogglAutomationApp.exe", "application/exe");
        }
    }
}
