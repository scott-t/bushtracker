using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
