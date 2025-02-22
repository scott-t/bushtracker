using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BushDiversTracker.Models.NonApi
{
    // Must stay in sync with SimService.cs simconnect definition
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SimLandingData
    {
        public double touchdown_bank;
        public double touchdown_heading_m;
        public double touchdown_heading_t;
        public double touchdown_lat;
        public double touchdown_lon;
        public double touchdown_velocity;
        public double touchdown_pitch;
    }
}
