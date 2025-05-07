using System.Text.Json.Serialization;

namespace TogglAutomationServer.Types;

public class TogglWebhookMetadata
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = null!;
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;
}
