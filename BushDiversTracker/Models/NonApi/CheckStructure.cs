using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BushDiversTracker.Models.NonApi
{
    public class CheckStructure
    {
        public string Aircraft { get; set; }
        public string AircraftType { get; set; }
        public double Fuel { get; set; }
        public double Payload { get; set; }
        public int Pax { get; set; }
        public double CurrentLat { get; set; }
        public double CurrentLon { get; set; }
    }
}
