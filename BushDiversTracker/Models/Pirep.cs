using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BushDiversTracker.Models
{
    class Pirep
    {
        [JsonPropertyName("pirep_id")]
        public string PirepId { get; set; }
        [JsonPropertyName("fuel_used")]
        public double? FuelUsed { get; set; }
        [JsonPropertyName("landing_rate")]
        public double? LandingRate { get; set; }
        [JsonPropertyName("landing_bank")]
        public double? TouchDownBank { get; set; }
        [JsonPropertyName("landing_lat")]
        public double? TouchDownLat { get; set; }
        [JsonPropertyName("landing_lon")]
        public double? TouchDownLon { get; set; }
        [JsonPropertyName("landing_pitch")]
        public double? TouchDownPitch { get; set; }
        [JsonPropertyName("block_off_time")]
        public string BlockOffTime { get; set; }
        [JsonPropertyName("block_on_time")]
        public string BlockOnTime { get; set; }
        [JsonPropertyName("distance")]
        public double Distance { get; set; }
    }
}
