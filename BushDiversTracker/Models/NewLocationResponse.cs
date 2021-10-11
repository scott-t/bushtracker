using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BushDiversTracker.Models
{
    class NewLocationResponse
    {
        [JsonPropertyName("icao")]
        public string Icao { get; set; }
    }
}
