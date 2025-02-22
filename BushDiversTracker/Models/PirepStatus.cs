using System.Text.Json.Serialization;

namespace BushDiversTracker.Models
{
    class PirepStatus
    {
        [JsonPropertyName("pirep_id")]
        public string PirepId { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}
