using System.Text.Json.Serialization;

namespace BushDiversTracker.Models
{
    class PirepCancellation
    {
        [JsonPropertyName("pirep_id")]
        public string PirepId { get; set; }
    }
}
