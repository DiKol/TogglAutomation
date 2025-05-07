using System.Text.Json.Serialization;

namespace TogglAutomationServer.Toggl;

public sealed class TogglSettings
{
    [JsonPropertyName("email")] public string Email { get; set; } = null!;
    [JsonPropertyName("password")] public string Password { get; set; } = null!;
    [JsonPropertyName("organizationId")] public string OrganizationId { get; set; } = null!;
    [JsonPropertyName("workspaceId")] public string WorkspaceId { get; set; } = null!;
    [JsonPropertyName("listenPort")] public int ListenPort { get; set; }
    [JsonPropertyName("webhookName")] public string WebHookName { get; set; } = null!;

    [JsonIgnore]
    public int? WebhookSubscriptionId { get; set; }
    [JsonIgnore]
    public string? WebhookSecret { get; set; }
}