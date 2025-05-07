using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text;
using System.Text.Json;
using TogglAutomationServer.Models;
using UskokDB;
using UskokDB.MySql;

namespace TogglAutomationServer.Controllers
{
    [Route("api/logs")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        [HttpPost("get")]
        public async Task<IActionResult> GetLogs([FromServices]MySqlConnection connection, [FromBody]GetLogsFilters filters)
        {
            if (!IsAuthorized(Request))
                return Unauthorized();

            FilterBuilder filterBuilder = Log.CreateFilterBuilder();
            if (filters.Extensions != null)
            {
                filterBuilder.AddOr("extension", filters.Extensions.Select(FilterOperand.EqualsOperand));
            }
            if(filters.LogTypes != null)
            {
                filterBuilder.AddOr("logType", filters.LogTypes.Select(x => FilterOperand.EqualsOperand(x)));
            }
            if(filters.To != null && filters.From != null)
                (filters.From, filters.To) = filters.To < filters.From ? (filters.To.Value, filters.From.Value) : (filters.From.Value, filters.To.Value);

            if(filters.To != null) filterBuilder.AddAnd("date", FilterOperand.LowerEqualsOperand(filters.To));
            if (filters.From != null) filterBuilder.AddAnd("date", FilterOperand.HigherEqualsOperand(filters.From));


            const int pageLimit = 20;

            int page = filters.Page.HasValue ? filters.Page.Value < 0 ? 0 : filters.Page.Value : 0;

            if (filters.Page != null) filterBuilder.SetOffset(page * pageLimit);
            filterBuilder.SetLimit(pageLimit);
            filterBuilder.OrderBy(true, "date");

            var logs = await filterBuilder.QueryAsync<Log>(connection);
            var count = await filterBuilder.CountAsync(connection);
            Console.WriteLine(filterBuilder.ToString());
            return Ok(new
            {
                logs,
                count,
                page,
                pageLimit
            });
        }


        public static bool IsAuthorized(HttpRequest request)
        {
            string? authHeader = request.Headers.Authorization;
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                if (encodedUsernamePassword != null)
                {
                    var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                    var username = decodedUsernamePassword.Split(':', 2)[0];
                    var password = decodedUsernamePassword.Split(':', 2)[1];

                    return username == "admin" && password == "toggl";
                }
            }

            return false;
        }
    }

    public class GetLogsFilters
    {
        public string[]? Extensions { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public LogType[]? LogTypes { get; set; }
        public int? Page { get; set; }
    }
}
