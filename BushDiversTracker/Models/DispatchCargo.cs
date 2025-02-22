using System.Text.Json.Serialization;

namespace BushDiversTracker.Models
{
    class DispatchCargo
    { 
        // This is the order it appears in the datagrid. Cargo must be last so it stretches full width
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("current_airport_id")]
        public string CurrentAirport { get; set; }
        [JsonPropertyName("arr_airport_id")]
        public string DestinationAirport { get; set; }
        [JsonPropertyName("contract_type")]
        public string ContractType { get; set; }
        [JsonPropertyName("cargo_qty")]
        public int CargoQty { get; set; }
        [JsonPropertyName("cargo")]
        public string Cargo { get; set; }

    }
}
