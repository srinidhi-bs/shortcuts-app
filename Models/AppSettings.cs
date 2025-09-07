using System.Text.Json.Serialization;

namespace ShortcutsApp.Models
{
    public class AppSettings
    {
        [JsonPropertyName("hotkey")]
        public HotKeySettings HotKey { get; set; } = new();

        [JsonPropertyName("shortcuts")]
        public List<ShortcutItem> Shortcuts { get; set; } = new();

        [JsonPropertyName("appearance")]
        public AppearanceSettings Appearance { get; set; } = new();
    }

    public class HotKeySettings
    {
        [JsonPropertyName("modifiers")]
        public List<string> Modifiers { get; set; } = new() { "Control" };

        [JsonPropertyName("key")]
        public string Key { get; set; } = "Space";

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
    }

    public class ShortcutItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("iconPath")]
        public string? IconPath { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; } = 0;

        // Recently used tracking properties
        [JsonPropertyName("usageCount")]
        public int UsageCount { get; set; } = 0;

        [JsonPropertyName("lastUsedAt")]
        public DateTime? LastUsedAt { get; set; }

        [JsonPropertyName("firstUsedAt")]
        public DateTime? FirstUsedAt { get; set; }
    }

    public class AppearanceSettings
    {
        [JsonPropertyName("gridColumns")]
        public int GridColumns { get; set; } = 6;

        [JsonPropertyName("iconSize")]
        public int IconSize { get; set; } = 64;

        [JsonPropertyName("popupOpacity")]
        public double PopupOpacity { get; set; } = 0.95;
    }
}