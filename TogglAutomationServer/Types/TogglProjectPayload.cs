using System.Text.Json.Serialization;

namespace TogglAutomationServer.Types;

public class TogglProjectPayload
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = null!;

    [JsonPropertyName("id")]
    public long ProjectId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}
