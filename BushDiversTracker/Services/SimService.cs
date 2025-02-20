using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows;

namespace BushDiversTracker.Services
{
    internal class SimService
    {
        MainWindow _mainWindow;

        public event EventHandler OnSimConnected;
        public event EventHandler OnSimDisconnected;
        public event EventHandler<SimData> OnSimDataReceived;
        public event EventHandler<SimLandingData> OnLandingDataReceived;

        private DispatcherTimer timer;
        private const int TIMER_INTERVAL = 5;
        internal static class CameraState
        {
            // "currently" these few are mapped the same
            //private static class FS2020
            //{
                public const int COCKPIT = 2;
                public const int CHASE = 3;
                public const int DRONE = 4;
            //}

            public static class FS2024
            {
                public const int MAIN_MENU = 32;
                public const int WORLD_MAP = 12;
            }
           
        }

        // sim connect setup variables
        SimConnect simConnect = null;
        const int WM_USER_SIMCONNECT = 0x0402;

        public enum SimVersion
        {
            FS2020,
            FS2024
        }
        private SimVersion? version = null;
        public SimVersion? Version { get => version; }

        public bool IsConnected { get => simConnect != null; }
        private int simCameraState = 0;
        public bool IsUserControlled { get => IsConnected && (simCameraState == CameraState.COCKPIT || simCameraState == CameraState.DRONE || simCameraState == CameraState.CHASE); }

        private enum DEFINITIONS
        {
            DataStruct,
            LandingStruct
        }

        private enum DAT_REQUESTS
        {
            SIM_MAIN_DATA,
            LANDING_DATA,
        }

        // items to set in sim
        private enum SET_DATA
        {
            ATC_ID
            // TODO: Fuel
            //LEFT_FUEL,
            //RIGHT_FUEL
        }

        // TODO: for events
        //enum EVENT_ID
        //{
        //    EVENT_PAUSED,
        //    EVENT_UNPAUSED,
        //}
        
        // Sim variables
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

        public SimService(MainWindow mainWindow )
        {
            _mainWindow = mainWindow;
        }

        protected HwndSource GetHWinSource() => PresentationSource.FromVisual((System.Windows.Media.Visual)_mainWindow) as HwndSource;

        private IntPtr WndProc(IntPtr hWnd, int iMsg, IntPtr hWParam, IntPtr hLParam, ref bool bHandled)
        {
            try
            {
                if (iMsg == 1026)
                {
                    //SimConnect simConnect = this.simConnect;
                    if (simConnect != null)
                    {
                        simConnect.ReceiveMessage();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                CloseConnection();
                return IntPtr.Zero;
            }
            return IntPtr.Zero;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (simConnect != null)
            {
                try
                {
                    simConnect.RequestDataOnSimObjectType(DAT_REQUESTS.SIM_MAIN_DATA, DEFINITIONS.DataStruct, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                }
                catch (COMException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
                OpenConnection();
        }

        /// <summary>
        /// Initiates a data request with the sim to setup the simvars to receive
        /// </summary>
        private void InitDataRequest()
        {
            try
            {
                simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvException);

                // define a data structure
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "Title", (string)null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "CAMERA STATE", "Enum", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "Plane Latitude", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "Plane Longitude", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "INDICATED ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "AIRSPEED TRUE", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "AIRSPEED INDICATED", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "VERTICAL SPEED", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE HEADING DEGREES MAGNETIC", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "G FORCE", "GForce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG COMBUSTION:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG COMBUSTION:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG COMBUSTION:3", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG COMBUSTION:4", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "MAX RATED ENGINE RPM", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "GENERAL ENG MAX REACHED RPM", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ZULU TIME", "seconds", SIMCONNECT_DATATYPE.INT32, 1E+09f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "LOCAL TIME", "seconds", SIMCONNECT_DATATYPE.INT32, 1E+09f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "SIM ON GROUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "SURFACE TYPE", "Enum", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ATC ID", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ATC TYPE", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "FUEL TOTAL QUANTITY", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "FUELSYSTEM TANK CAPACITY:1", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED); // NEW FUEL SYSTEM simvar borked in MSFS2024, use this to test
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "UNUSABLE FUEL TOTAL QUANTITY", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "OVERSPEED WARNING", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "UNLIMITED FUEL", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PAYLOAD STATION COUNT", "Number", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PAYLOAD STATION WEIGHT:1", "Pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "MAX G FORCE", "Gforce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "MIN G FORCE", "Gforce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                //simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "GENERAL ENG DAMAGE PERCENT", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                //simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "FLAP DAMAGE BY SPEED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                //simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR DAMAGE BY SPEED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                //simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "FLAP SPEED EXCEEDED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                //simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR SPEED EXCEEDED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                //simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG MANIFOLD PRESSURE", "inHG", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG FUEL FLOW GPH", "Gallons per hour", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ATC MODEL", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "TOTAL WEIGHT", "Pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                //simConnect.AddToDataDefinition(SET_DATA.RIGHT_FUEL, "FUEL TANK RIGHT MAIN QUANTITY", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                // IMPORTANT: register it with the simconnect managed wrapper marshaller
                simConnect.RegisterDataDefineStruct<SimData>(DEFINITIONS.DataStruct);

                simConnect.AddToDataDefinition(DEFINITIONS.LandingStruct, "PLANE TOUCHDOWN BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.LandingStruct, "PLANE TOUCHDOWN HEADING DEGREES MAGNETIC", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.LandingStruct, "PLANE TOUCHDOWN HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.LandingStruct, "PLANE TOUCHDOWN LATITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.LandingStruct, "PLANE TOUCHDOWN LONGITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.LandingStruct, "PLANE TOUCHDOWN NORMAL VELOCITY", "Feet per minute", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.LandingStruct, "PLANE TOUCHDOWN PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.RegisterDataDefineStruct<SimLandingData>(DEFINITIONS.LandingStruct);

                // catch a simobject data request
                simConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(simConnect_OnRecvSimobjectDataBytype);

                simConnect.RequestDataOnSimObject(DAT_REQUESTS.LANDING_DATA, DEFINITIONS.LandingStruct, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                simConnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(simConnect_OnRecvSimobjectData);

            }
            catch (COMException ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        /// <summary>
        /// Triggered when communication with simconnect has been opened, sets the connection status
        /// </summary>
        /// <param name="sender">The SimConnect library.</param>
        /// <param name="data">Data received from sim.</param>
        private void simConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            if (data.szApplicationName == "KittyHawk")
            {
                version = SimVersion.FS2020;
                HelperService.WriteToLog("Connected to FS2020");
            }
            else if (data.szApplicationName == "SunRise")
            {
                version = SimVersion.FS2024;
                HelperService.WriteToLog("Connected to FS2024");
            }

            OnSimConnected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Triggered when communication with simconnect has been closed, stops tracking
        /// </summary>
        /// <param name="sender">The SimConnect library.</param>
        /// <param name="data">Data received from sim.</param>
        private void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            CloseConnection();
            OnSimDisconnected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Triggered when an exception within simconnect library ocurrs, sets the connection status
        /// </summary>
        /// <param name="sender">The SimConnect library.</param>
        /// <param name="data">Data received from sim.</param>
        private void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            simConnect = null;
            OnSimDisconnected?.Invoke(this, EventArgs.Empty);
        }

        // TODO: Someday - handle pause of sim to make time of flight more accurate
        // Simconnect does not report this correctly for MSFS

        //private void simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        //{
        //    Console.WriteLine("Event received");
        //    switch (recEvent.uEventID)
        //    {
        //        case (uint)EVENT_ID.EVENT_PAUSED:
        //            Console.WriteLine("Paused");
        //            break;
        //        case (uint)EVENT_ID.EVENT_UNPAUSED:
        //            Console.WriteLine("Unpaused");
        //            break;
        //    }

        //}

        /// <summary>
        /// Triggered on each data receipt from simconnect, handles logic of sim data
        /// </summary>
        /// <param name="sender">The SimConnect library.</param>
        /// <param name="data">Data received from sim.</param>
        private void simConnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            if (data.dwRequestID == (uint)DAT_REQUESTS.SIM_MAIN_DATA)
            {
                SimData data1 = (SimData)data.dwData[0];

                if (data1.fuelsystem_tank1_capacity > 0)
                {
                    // new fuel system treats total fuel qty excluding unusable fuel
                    // old fuel system this var includes unusable fuel
                    data1.fuel_qty += data1.unusable_fuel_qty;
                }

                // Increase resolution in case of water ditch near ground
                if (data1.plane_altitude <= 750 && timer.Interval.Seconds > 1)
                    timer.Interval = TimeSpan.FromSeconds(1.0);
                else if (data1.plane_altitude >= 1500 && timer.Interval.Seconds < 2)
                    timer.Interval = TimeSpan.FromSeconds(TIMER_INTERVAL);

                simCameraState = data1.camera_state;

                OnSimDataReceived?.Invoke(this, data1);
            }
        }

        private void simConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == (uint)DAT_REQUESTS.LANDING_DATA)
            {
                SimLandingData data1 = (SimLandingData)data.dwData[0];
                OnLandingDataReceived?.Invoke(this, data1);
            }
        }

        /// <summary>
        /// Starts a connection with SimConnect
        /// </summary>
        public void OpenConnection()
        {
            try
            {
                _mainWindow.SetStatusMessage("Connecting to sim");

                GetHWinSource().AddHook(new HwndSourceHook(WndProc));

                simConnect = new SimConnect("Managed Data Request", GetHWinSource().Handle, WM_USER_SIMCONNECT, null, 0);
                //simConnect.SubscribeToSystemEvent(EVENT_ID.EVENT_PAUSED, "Paused");
                //simConnect.SubscribeToSystemEvent(EVENT_ID.EVENT_UNPAUSED, "Unpaused");
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simConnect_OnRecvOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);
                //simConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(simconnect_OnRecvEvent);

                InitDataRequest();

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(TIMER_INTERVAL);
                timer.Tick += new EventHandler(Timer_Tick);
                timer.Start();
            }
            catch (COMException ex)
            {
                HelperService.WriteToLog($"Issue connecting to sim: {ex.Message}");
                _mainWindow.SetStatusMessage($"Issue connecting to sim: {ex.Message}", MainWindow.MessageState.Error);
            }
        }

        /// <summary>
        /// Closes a connection with SimConnect
        /// </summary>
        public void CloseConnection()
        {
            if (simConnect != null)
            {
                //simConnect.UnsubscribeFromSystemEvent(EVENT_ID.EVENT_PAUSED);
                //simConnect.UnsubscribeFromSystemEvent(EVENT_ID.EVENT_UNPAUSED);
                simConnect.Dispose();
                simConnect = null;
            }

            if (timer != null)
                timer.Stop();

            OnSimDisconnected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Starts request for data with simconnect
        /// </summary>
        public void StartTracking()
        {
            try
            {
                InitDataRequest();
            }
            catch (COMException ex)
            {
                HelperService.WriteToLog($"Issue getting update from sim: {ex.Message}");
            }
        }

        public void StartFlight()
        {
            timer.Stop();
            // Might still get a double-fire if the dispatch is ready to send or even if the timer has ticked but response yet to be received - "shouldn't" be an issue
            Timer_Tick(null, null);
            timer.Start();
        }

        /// <summary>
        /// Sends text to display on Sim - currently broken in simconnect
        /// </summary>
        /// <param name="text">string to be sent to sim</param>
        public void SendTextToSim(string text)
        {
            simConnect.Text(SIMCONNECT_TEXT_TYPE.PRINT_BLACK, 5, SIMCONNECT_EVENT_FLAG.DEFAULT, text);
        }

    }
}
