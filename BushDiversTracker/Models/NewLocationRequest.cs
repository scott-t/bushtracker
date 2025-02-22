using System.Text.Json.Serialization;

namespace BushDiversTracker.Models
{
    class NewLocationRequest
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        [JsonPropertyName("lon")]
        public double Lon { get; set; }
        [JsonPropertyName("pirep_id")]
        public string PirepId { get; set; }
    }
}
