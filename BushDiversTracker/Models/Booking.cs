using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BushDiversTracker.Models
{
    class Booking
    {
        [JsonPropertyName("flight_number")]
        public int FlightNumber { get; set; }
        [JsonPropertyName("dep_airport_id")]
        public string Departure { get; set; }
        [JsonPropertyName("arr_airport_id")]
        public string Arrival { get; set; }
        [JsonPropertyName("name")]
        public string Aircraft { get; set; }
        [JsonPropertyName("registration")]
        public string Registration { get; set; }
        [JsonPropertyName("planned_cruise_altitude")]
        public int CruiseAltitude { get; set; }
        [JsonPropertyName("planned_fuel")]
        public decimal PlannedFuel { get; set; }
        [JsonPropertyName("cargo")]
        public int Cargo { get; set; }
        [JsonPropertyName("cargo_name")]
        public string CargoType { get; set; }
        [JsonPropertyName("pax")]
        public int Pax { get; set; }
        [JsonPropertyName("pax_name")]
        public string PaxType { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
