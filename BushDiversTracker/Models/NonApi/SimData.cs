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
    public struct SimData
    {
        // variables to bind to simconnect simvars
        public int camera_state;
        public double latitude;
        public double longitude;
        public double indicated_altitude;
        public double plane_altitude;
        public double alt_above_ground;
        public double ac_pitch;
        public double ac_bank;
        public double airspeed_true;
        public double airspeed_indicated;
        public double surface_rel_groundspeed;
        public double vspeed;
        public double heading_m;
        public double heading_t;
        public double gforce;
        public int eng1_combustion;
        public int eng2_combustion;
        public int eng3_combustion;
        public int eng4_combustion;

        public int zulu_time;
        public int local_time;
        public int on_ground;
        public int surface_type;
        public double fuel_qty;
        public double fuelsystem_tank1_capacity;
        public double unusable_fuel_qty;
        public int fuel_flow;

        public readonly bool EnginesRunning => eng1_combustion > 0 || eng2_combustion > 0 ||
            eng3_combustion > 0 || eng4_combustion > 0;

        public readonly bool IsNull => latitude == 0.0 && longitude == 0.0 && zulu_time == 0;
    };
}
