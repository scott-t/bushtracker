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
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;
        public int camera_state;
        public double latitude;
        public double longitude;
        public double indicated_altitude;
        public double plane_altitude;
        public double ac_pitch;
        public double ac_bank;
        public double airspeed_true;
        public double airspeed_indicated;
        public double vspeed;
        public double heading_m;
        public double heading_t;
        public double gforce;
        public int eng1_combustion;
        public int eng2_combustion;
        public int eng3_combustion;
        public int eng4_combustion;
        public double aircraft_max_rpm;
        public double max_rpm_attained;
        public int zulu_time;
        public int local_time;
        public int on_ground;
        public int surface_type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string atcId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string atcType;
        public double fuel_qty;
        public double fuelsystem_tank1_capacity;
        public double unusable_fuel_qty;
        public int is_overspeed;
        public int is_unlimited;
        public int payload_station_count;
        public double payload_station_weight;
        public double max_g;
        public double min_g;
        //public double eng_damage_perc;
        //public int flap_damage;
        //public int gear_damage;
        //public int flap_speed_exceeded;
        //public int gear_speed_exceeded;
        //public int eng_mp;
        public int fuel_flow;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string atcModel;
        public double total_weight;
    };
}
