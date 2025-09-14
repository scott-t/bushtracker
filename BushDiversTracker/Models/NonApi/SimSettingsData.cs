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
    public struct SimSettingsData
    {
        public const int MAX_PAYLOAD_STATIONS = 15;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MarshalledString
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string value;

            public override string ToString()
            {
                return value;
            }
        };


        // variables to bind to simconnect simvars
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string aircraft_name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string atcId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string atcType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string atcModel;

        public int is_unlimited_fuel;
        public int is_slew_mode;

        public int payload_station_count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_PAYLOAD_STATIONS)]
        public MarshalledString[] payload_station_name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_PAYLOAD_STATIONS)]
        public double[] payload_station_weight;
        public double total_weight;
        
    };
}
