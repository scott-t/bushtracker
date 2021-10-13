using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BushDiversTracker.Models
{
    class FlightLog
    {
        [JsonPropertyName("pirep_id")]
        public string PirepId {get; set;}
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        [JsonPropertyName("lon")]
        public double Lon { get; set; }
        [JsonPropertyName("heading")]
        public int Heading { get; set; }
        [JsonPropertyName("altitude")]
        public int Altitude { get; set; }
        [JsonPropertyName("indicated_speed")]
        public int IndicatedSpeed { get; set; }
        [JsonPropertyName("ground_speed")]
        public int GroundSpeed { get; set; }
        [JsonPropertyName("fuel_flow")]
        public double FuelFlow { get; set; }
        [JsonPropertyName("vs")]
        public double VS { get; set; }
        [JsonPropertyName("sim_time")]
        public DateTime SimTime { get; set; }
        [JsonPropertyName("zulu_time")]
        public DateTime ZuluTime { get; set; }
        [JsonPropertyName("distance")]
        public double Distance { get; set; }
    }
}
