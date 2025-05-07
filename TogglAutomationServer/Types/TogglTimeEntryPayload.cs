using System.Text.Json.Serialization;

namespace TogglAutomationServer.Types;

public class TogglTimeEntryPayload
{
    [JsonPropertyName("project_id")]
    public long ProjectId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("duration")]
    public long Duration { get; set; }
}
