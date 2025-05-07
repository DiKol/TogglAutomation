using System.Text.Json.Serialization;

namespace TogglAutomationServer.Toggl;

public class Project
{
    [JsonPropertyName("id")]
    public long ProjectId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("color")]
    public string Color { get; set; } = null!;


    private record ProjectApiCall([property:JsonPropertyName("id")]long ProjectId, [property:JsonPropertyName("name")]string Name, [property:JsonPropertyName("color")]string Color);
    public static async Task<Project?> GetProjectAsync(HttpClient client, long projectId, string workspaceId)
    {
        try
        {
            var response = await client.GetFromJsonAsync<ProjectApiCall>($"https://api.track.toggl.com/api/v9/workspaces/{workspaceId}/projects/{projectId}");
            if (response == null) return null;

            return new Project()
            {
                Color = response.Color,
                Name = response.Name,
                ProjectId = projectId,
            };
        }
        catch
        {
            return null;
        }
    }
}
