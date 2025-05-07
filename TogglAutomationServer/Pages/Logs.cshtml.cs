using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using System.Net;
using System.Text;
using System.Text.Json;
using TogglAutomationServer.Controllers;
using TogglAutomationServer.Models;
using UskokDB;
using UskokDB.MySql;

namespace TogglAutomationServer.Pages
{
    public class LogsPageModel : PageModel
    {
        public string[] Extensions { get; set; } = [];
        public async Task OnGet([FromServices]MySqlConnection connection)
        {
            if (!LogsController.IsAuthorized(Request))
            {
                Response.Headers.WWWAuthenticate = "Basic";
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }
            var extensions = await connection.QueryAsync<GetAllExtensionsQuery>("SELECT extension FROM Log GROUP BY extension");
            Extensions = extensions.Select(x => x.Extension).ToArray();
        }

        private class GetAllExtensionsQuery
        {
            public string Extension { get; set; } = null!;
        }
    }
}
