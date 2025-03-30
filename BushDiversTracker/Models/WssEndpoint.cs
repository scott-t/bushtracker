using System;
using System.Text.Json.Serialization;

namespace BushDiversTracker.Models
{
    internal class WssEndpoint
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }
        [JsonPropertyName("host")]
        public string Host { get; set; }
        [JsonPropertyName("port")]
        public int Port { get; set; }
        [JsonPropertyName("scheme")]
        public string Scheme { get; set; }
        [JsonPropertyName("cluster")]
        public string Cluster { get; set; }
    }
}
