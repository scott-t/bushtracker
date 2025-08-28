using Microsoft.FlightSimulator.SimConnect;
using BushDiversTracker.Models.Enums;
using BushDiversTracker.Models.NonApi;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows;
using System.Linq;

namespace BushDiversTracker.Services
{
    internal class SimServiceMSFS : ISimService
    {
        MainWindow _mainWindow;

        public event EventHandler OnSimConnected;
        public event EventHandler OnSimDisconnected;
        public event EventHandler<SimData> OnSimDataReceived;
        public event EventHandler<SimSettingsData> OnFlightSettingsReceived;
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
                
        private SimVersion? version = null;
        public SimVersion? Version { get => version; }

        public bool IsConnected { get => simConnect != null; }
        private int simCameraState = 0;
        public bool IsUserControlled { get => IsConnected && (simCameraState == CameraState.COCKPIT || simCameraState == CameraState.DRONE || simCameraState == CameraState.CHASE); }
        public bool SendSimText { get; set; }

        private enum DEFINITIONS
        {
            DataStruct,
            LandingStruct,
            FlightSettingsStruct,

            SetSlew

        }

        private enum DAT_REQUESTS
        {
            SIM_MAIN_DATA,
            LANDING_DATA,
            FLIGHT_SETTINGS_DATA,

            SET_SLEW
        }

        // TODO: for events
        //enum EVENT_ID
        //{
        //    EVENT_PAUSED,
        //    EVENT_UNPAUSED,
        //}
        
        // Sim variables

        public SimServiceMSFS(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            SendSimText = _mainWindow.chkTextToSim.IsChecked == true;
        }

        protected HwndSource GetHWinSource() => HwndSource.FromVisual((System.Windows.Media.Visual)_mainWindow) as HwndSource;
        //HwndSource.FromHwnd((new System.Windows.Interop.WindowInteropHelper(_mainWindow)).Handle);
        HwndSourceHook sourceHook = null;

        private IntPtr WndProc(IntPtr hWnd, int iMsg, IntPtr hWParam, IntPtr hLParam, ref bool bHandled)
        {
            try
            {
                if (iMsg == 1026)
                {
                    //SimConnect simConnect = this.simConnect;
                    simConnect?.ReceiveMessage();
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
                simConnect.AddToDataDefinition(DEFINITIONS.FlightSettingsStruct, "Title", (string)null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.FlightSettingsStruct, "ATC ID", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.FlightSettingsStruct, "ATC TYPE", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.FlightSettingsStruct, "ATC MODEL", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.FlightSettingsStruct, "UNLIMITED FUEL", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.FlightSettingsStruct, "IS SLEW ACTIVE", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.FlightSettingsStruct, "PAYLOAD STATION COUNT", "Number", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                for (int i = 1; i <= SimSettingsData.MAX_PAYLOAD_STATIONS; i++)
                    simConnect.AddToDataDefinition(DEFINITIONS.FlightSettingsStruct, $"PAYLOAD STATION WEIGHT:{i}", "Pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.RegisterDataDefineStruct<SimSettingsData>(DEFINITIONS.FlightSettingsStruct);

                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "CAMERA STATE", "Enum", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "Plane Latitude", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "Plane Longitude", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "INDICATED ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE ALT ABOVE GROUND", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "AIRSPEED TRUE", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "AIRSPEED INDICATED", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "SURFACE RELATIVE GROUND SPEED", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "VERTICAL SPEED", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE HEADING DEGREES MAGNETIC", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "PLANE HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "G FORCE", "GForce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG COMBUSTION:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG COMBUSTION:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG COMBUSTION:3", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG COMBUSTION:4", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ZULU TIME", "seconds", SIMCONNECT_DATATYPE.INT32, 1E+09f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "LOCAL TIME", "seconds", SIMCONNECT_DATATYPE.INT32, 1E+09f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "SIM ON GROUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "SURFACE TYPE", "Enum", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "FUEL TOTAL QUANTITY", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "FUELSYSTEM TANK CAPACITY:1", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED); // NEW FUEL SYSTEM simvar borked in MSFS2024, use this to test
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "UNUSABLE FUEL TOTAL QUANTITY", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.DataStruct, "ENG FUEL FLOW GPH", "Gallons per hour", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

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
                simConnect.RequestDataOnSimObject(DAT_REQUESTS.FLIGHT_SETTINGS_DATA, DEFINITIONS.FlightSettingsStruct, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                simConnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(simConnect_OnRecvSimobjectData);

                simConnect.AddToDataDefinition(DEFINITIONS.SetSlew, "IS SLEW ALLOWED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

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
            else if (data.dwRequestID == (uint)DAT_REQUESTS.FLIGHT_SETTINGS_DATA)
            {
                SimSettingsData data1 = (SimSettingsData)data.dwData[0];
                data1.total_weight = data1.payload_station_weight.Take(data1.payload_station_count).Sum();
                OnFlightSettingsReceived?.Invoke(this, data1);
            }
        }

        /// <summary>
        /// Starts a connection with SimConnect
        /// </summary>
        public void OpenConnection()
        {
            // Already connected
            if (simConnect != null)
                return; 

            try
            {
                _mainWindow.SetStatusMessage("Connecting to sim");

                if (sourceHook == null)
                {
                    sourceHook = new HwndSourceHook(WndProc);
                    GetHWinSource().AddHook(sourceHook);
                }

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

                OnSimConnected?.Invoke(this, EventArgs.Empty);
            }
            catch (COMException ex)
            {
                //HelperService.WriteToLog($"Issue connecting to sim: {ex.Message}");
                _mainWindow.SetStatusMessage($"Issue connecting to sim. Is sim running?", MainWindow.MessageState.Error);
                OnSimDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Closes a connection with SimConnect
        /// </summary>
        public void CloseConnection()
        {
            if (simConnect != null)
            {
                GetHWinSource().RemoveHook(sourceHook);
                sourceHook = null;
                //simConnect.UnsubscribeFromSystemEvent(EVENT_ID.EVENT_PAUSED);
                //simConnect.UnsubscribeFromSystemEvent(EVENT_ID.EVENT_UNPAUSED);
                simConnect.Dispose();
                simConnect = null;
            }

            timer?.Stop();

            OnSimDisconnected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Allow or block slew mode
        /// </summary>
        /// <param name="strictMode"></param>
        public void SetStrictMode(bool strictMode)
        {
            if (simConnect == null)
                return;

            try
            {
                // Set slew mode
                bool isSlewAllowed = !strictMode;// ? 0 : 1;
                simConnect.SetDataOnSimObject(DEFINITIONS.SetSlew, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, isSlewAllowed);
            }
            catch (COMException ex)
            {
                HelperService.WriteToLog($"Issue setting strict mode: {ex.Message}");
            }
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
            if (SendSimText)
                simConnect.Text(SIMCONNECT_TEXT_TYPE.PRINT_BLACK, 5, SIMCONNECT_EVENT_FLAG.DEFAULT, text);
        }

    }
}
