using System.Text.Json.Serialization;

namespace BushDiversTracker.Models.NonApi
{
    internal class InstalledAddon
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("creator")]
        public string Creator { get; set; }
        [JsonPropertyName("package_version")]
        public System.Version Version { get; set; }

        [JsonIgnore]
        public string Filename { get; set; }
        [JsonPropertyName("content_type")]
        public string AddonTypeStr { get; set; }

    }
}
