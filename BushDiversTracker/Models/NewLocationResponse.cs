using System.Text.Json.Serialization;

namespace BushDiversTracker.Models
{
    class NewLocationResponse
    {
        [JsonPropertyName("icao")]
        public string Icao { get; set; }
        [JsonPropertyName("lat")]
        public decimal Lat { get; set; }
        [JsonPropertyName("lon")]
        public decimal Lon { get; set; }
    }
}
