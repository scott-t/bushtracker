using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BushDiversTracker.Models
{
    class DispatchCargo
    {
        [JsonPropertyName("contract_type")]
        public string ContractType { get; set; }
        [JsonPropertyName("current_airport_id")]
        public string CurrentAirport { get; set; }
        [JsonPropertyName("arr_airport_id")]
        public string DestinationAirport { get; set; }
        [JsonPropertyName("cargo")]
        public string Cargo { get; set; }
        [JsonPropertyName("cargo_qty")]
        public int CargoQty { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }

    }
}
