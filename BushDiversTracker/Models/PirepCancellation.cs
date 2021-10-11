using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BushDiversTracker.Models
{
    class PirepCancellation
    {
        [JsonPropertyName("pirep_id")]
        public string PirepId { get; set; }
    }
}
