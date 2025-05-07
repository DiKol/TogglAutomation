using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace TogglAutomationApp
{
    public class Project
    {
        [JsonPropertyName("color")]
        public string Color { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("id")]
        public int ProjectId { get; set; }
    }

    public class ProjectSelectInfo
    {
        [JsonPropertyName("inCall")]
        public Project? InCall { get; set; }

        [JsonPropertyName("beforeCall")]
        public Project? BeforeCall { get; set; }
    }
}
