using BushDiversTracker.Models;
using BushDiversTracker.Models.Enums;
using BushDiversTracker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Interop;
using BushDiversTracker.Models.NonApi;
using System.Globalization;
using AutoUpdaterDotNET;
using System.Reflection;

namespace BushDiversTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        APIService _api;

        public MainWindow()
        {
            InitializeComponent();
            txtKey.Text = Properties.Settings.Default.Key;
            _api = new APIService();
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                AutoUpdater.Start("https://storage.googleapis.com/bush-divers.appspot.com/bushtracker-info.xml");
            }
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            lblVersion.Content = version;
        }

#region Sim_Connect

        CultureInfo Eng = CultureInfo.GetCultureInfo("en-GB");

        private DispatcherTimer timer;

        // Bush Tracker variables
        protected bool bDispatch = false;
        private bool bConnected = false;
        private bool bFlightTracking = false;
        private bool bEndFlight = false;
        private bool bReady = false;
        private bool flag = false;
        private bool bFlightCompleted = false;
        private bool bFirstData = true;
        private bool bLastEngineStatus;
        private double startLat;
        private double startLon;
        private double endLat;
        private double endLon;
        private double startFuelQty;
        private double endFuelQty;
        private string startTime;
        private string endTime;
        protected int flightStatus;
        protected double lastHeading;
        protected double lastAltitude;
        protected double landingRate;
        protected double landingBank;
        protected double landingPitch;
        protected double landingLat;
        protected double landingLon;
        protected double lastVs;
        protected DateTime dataLastSent;

        // sim connect setup
        SimConnect simConnect = null;
        const int WM_USER_SIMCONNECT = 0x0402;

        enum DEFINITIONS
        {
            Struct1,
        }
        enum DAT_REQUESTS
        {
            REQUEST_1,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct Struct1
        {
            // this is how you declare a fixed size string
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string title;
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
            public double eng1_rpm;
            public double eng2_rpm;
            public double eng3_rpm;
            public double eng4_rpm;
            public double eng5_rpm;
            public double eng6_rpm;
            public double aircraft_max_rpm;
            public double max_rpm_attained;
            public int zulu_time;
            public int local_time;
            public int on_ground;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string atcId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string atcType;
            public double fuel_qty;
            public int is_overspeed;
            public int is_unlimited;
            public int payload_station_count;
            public double payload_station_weight;
            public double touchdown_bank;
            public double touchdown_heading_m;
            public double touchdown_heading_t;
            public double touchdown_lat;
            public double touchdown_lon;
            public double touchdown_velocity;
            public double touchdown_pitch;
            public double max_g;
            public double min_g;
            public double eng_damage_perc;
            public int flap_damage;
            public int gear_damage;
            public int flap_speed_exceeded;
            public int gear_speed_exceeded;
            public int eng_mp;
            public int fuel_flow;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string atcModel;
            public double total_weight;


        };

        protected HwndSource GetHWinSource() => PresentationSource.FromVisual((Visual)this) as HwndSource;

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
                    simConnect.RequestDataOnSimObjectType(DAT_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                    return;
                }
                catch (COMException ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
            }
            OpenConnection();
        }

        private void initDataRequest()
        {
            try
            {
                simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvException);

                // define a data structure
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "Title", (string)null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Latitude", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Longitude", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "INDICATED ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRSPEED TRUE", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRSPEED INDICATED", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "VERTICAL SPEED", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES MAGNETIC", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "G FORCE", "GForce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG PCT MAX RPM:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG PCT MAX RPM:2", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG PCT MAX RPM:3", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG PCT MAX RPM:4", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG PCT MAX RPM:5", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG PCT MAX RPM:6", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "MAX RATED ENGINE RPM", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG MAX REACHED RPM", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "ZULU TIME", "seconds", SIMCONNECT_DATATYPE.INT32, 1E+09f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "LOCAL TIME", "seconds", SIMCONNECT_DATATYPE.INT32, 1E+09f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "SIM ON GROUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "ATC ID", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "ATC TYPE", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "FUEL TOTAL QUANTITY", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "OVERSPEED WARNING", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "UNLIMITED FUEL", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PAYLOAD STATION COUNT", "Number", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PAYLOAD STATION WEIGHT:1", "Pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE TOUCHDOWN BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE TOUCHDOWN HEADING DEGREES MAGNETIC", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE TOUCHDOWN HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE TOUCHDOWN LATITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE TOUCHDOWN LONGITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE TOUCHDOWN NORMAL VELOCITY", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE TOUCHDOWN PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "MAX G FORCE", "Gforce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "MIN G FORCE", "Gforce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG DAMAGE PERCENT", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "FLAP DAMAGE BY SPEED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR DAMAGE BY SPEED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "FLAP SPEED EXCEEDED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR SPEED EXCEEDED", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG MANIFOLD PRESSURE", "inHG", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG FUEL FLOW GPH", "Gallons per hour", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "ATC MODEL", (string)null, SIMCONNECT_DATATYPE.STRING8, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "TOTAL WEIGHT", "Pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                // IMPORTANT: register it with the simconnect managed wrapper marshaller
                simConnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);

                // catch a simobject data request
                simConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(simConnect_OnRecvSimobjectDataBytype);

            }
            catch (COMException ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private void simConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            elConnection.Fill = Brushes.Green;
            elConnection.Stroke = Brushes.Green;
        }

        private void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            CloseConnection();
            if (!bFlightCompleted && bFlightTracking) StopTracking();
        }

        private void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            elConnection.Fill = Brushes.Red;
            elConnection.Stroke = Brushes.Red;
            simConnect = null;
        }

        private void simConnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            if (data.dwRequestID == 0U)
            {
                Struct1 data1 = (Struct1)data.dwData[0];
                // engine status
                flag = (data1.eng1_rpm > 1.0 || data1.eng2_rpm > 1.0 || data1.eng3_rpm > 1.0 || data1.eng4_rpm > 1.0 || data1.eng5_rpm > 1.0 ? 1 : (data1.eng6_rpm > 1.0 ? 1 : 0)) != 0;

                if (!bFlightTracking)
                    return;

                if (!bReady)
                {
                    var startCheckStructure = new CheckStructure
                    {
                        Aircraft = data1.title,
                        AircraftType = data1.atcType,
                        Fuel = data1.fuel_qty,
                        Payload = data1.total_weight,
                        Pax = 0,
                        CurrentLat = data1.latitude,
                        CurrentLon = data1.longitude
                    };
                    var status = CheckReadyForStart(startCheckStructure);
                    if (status)
                    {
                        bReady = true;
                        btnStart.Visibility = Visibility.Collapsed;
                        btnStop.Visibility = Visibility.Visible;
                        lblStatusText.Text = "Ready to Start";

                    } else
                    {
                        bReady = false;
                        lblStart.Visibility = Visibility.Collapsed;
                        return;
                    }
                    lblStart.Visibility = Visibility.Collapsed;
                }

                if (flag && bFirstData)
                {
                    bLastEngineStatus = false;
                    bFirstData = false;
                }



                // Checks for start of flight and sets offblocks time
                if (flag && Convert.ToBoolean(data1.on_ground) && !bLastEngineStatus)
                {
                    startLat = data1.latitude;
                    startLon = data1.longitude;
                    startTime = HelperService.SetZuluTime(data1.zulu_time).ToString("yyyy-MM-dd HH:mm:ss");
                    startFuelQty = data1.fuel_qty;
                    flightStatus = Convert.ToInt32(PirepStatusType.BOARDING);
                    lblStatusText.Text = "Pre-flight|Loading";
                    SendTextToSim("Bush Tracker Status: Pre-Flight - Ready");
                }

                // check for take off
                if (flightStatus == Convert.ToInt32(PirepStatusType.BOARDING) && !Convert.ToBoolean(data1.on_ground))
                {
                    flightStatus = Convert.ToInt32(PirepStatusType.DEPARTED);
                    lblStatusText.Text = "Departed";
                    
                    SendTextToSim("Bush Tracker Status: Departed - Have a good flight!");
                }

                // check for landed
                if (flightStatus == Convert.ToInt32(PirepStatusType.DEPARTED) && Convert.ToBoolean(data1.on_ground))
                {
                    flightStatus = Convert.ToInt32(PirepStatusType.APPROACH);
                    lblStatusText.Text = "Landed";
                    btnEndFlight.IsEnabled = true;
                    SendTextToSim("Bush Tracker Status: Landed");
                }

                if (bEndFlight)
                {
                    if (!flag && Convert.ToBoolean(data1.on_ground))
                    {
                        bFlightCompleted = true;
                        bFlightTracking = false;
                        flightStatus = Convert.ToInt32(PirepStatusType.ARRIVED);
                        lblStatusText.Text = "Flight ended";
                        SendTextToSim("Bush Tracker Status: Flight ended - Thanks for working with Bush Divers");

                        endFuelQty = data1.fuel_qty;
                        endLat = data1.latitude;
                        endLon = data1.longitude;
                        endTime = HelperService.SetZuluTime(data1.zulu_time).ToString("yyyy-MM-dd HH:mm:ss");
                        landingRate = data1.touchdown_velocity;
                        landingPitch = data1.touchdown_pitch;
                        landingBank = data1.touchdown_bank;
                        landingLat = data1.touchdown_lat;
                        landingLon = data1.touchdown_lon;

                        // btnStop.Visibility = Visibility.Visible;
                        btnSubmit.IsEnabled = true;

                        bLastEngineStatus = flag;
                        bEndFlight = false;
                        btnEndFlight.IsEnabled = false;
                        lblEnd.Visibility = Visibility.Hidden;
                    } else
                    {
                        MessageBox.Show("You must be on the ground with engines off to end your flight", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Warning);
                        bEndFlight = false;
                        btnEndFlight.IsEnabled = true;
                        lblEnd.Visibility = Visibility.Hidden;
                    }
                }
                

                // Send data to api
                var headingChanged = HelperService.CheckForHeadingChange(lastHeading, data1.heading_m);
                var altChanged = HelperService.CheckForAltChange(lastAltitude, data1.indicated_altitude);

                if (headingChanged || altChanged)
                {
                    SendFlightLog(data1);
                    dataLastSent = DateTime.UtcNow;
                } else if (DateTime.UtcNow > dataLastSent.AddSeconds(60))
                {
                    SendFlightLog(data1);
                    dataLastSent = DateTime.UtcNow;
                }

                bLastEngineStatus = flag;

                lastAltitude = data1.indicated_altitude;
                lastHeading = data1.heading_m;
                lastVs = data1.vspeed;
            }
        }

        public void OpenConnection()
        {
            try
            {
                GetHWinSource().AddHook(new HwndSourceHook(WndProc));

                simConnect = new SimConnect("Managed Data Request", GetHWinSource().Handle, WM_USER_SIMCONNECT, null, 0);
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simConnect_OnRecvOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);
                initDataRequest();

                bConnected = true;
                if (bDispatch) btnStart.IsEnabled = true;

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(10.0);
                timer.Tick += new EventHandler(Timer_Tick);
                timer.Start();
            }
            catch (COMException ex)
            {
                HelperService.WriteToLog($"Issue connecting to sim: {ex.Message}");
                lblErrorText.Text = $"Issue connecting to sim: {ex.Message}";
            }
        }

        public void CloseConnection()
        {
            if (simConnect != null)
            {
                simConnect.Dispose();
                simConnect = null;
            }
            this.timer.Stop();
            elConnection.Fill = Brushes.Red;
            elConnection.Stroke = Brushes.Red;
            btnConnect.IsEnabled = true;
            btnStart.IsEnabled = false;
            bConnected = false;
        }

        public void StartTracking()
        {
            try
            {
                initDataRequest();
            }
            catch (COMException ex)
            {
                HelperService.WriteToLog($"Issue getting update from sim: {ex.Message}");
                lblErrorText.Text = $"Issue getting update from sim: {ex.Message}";
            }
        }

        public async void SendFlightLog(Struct1 d)
        {
            var log = new FlightLog()
            {
                PirepId = txtPirep.Text,
                Lat = d.latitude,
                Lon = d.longitude,
                Distance = 0,
                Heading = Convert.ToInt32(d.heading_m),
                Altitude = Convert.ToInt32(d.indicated_altitude),
                IndicatedSpeed = Convert.ToInt32(d.airspeed_indicated),
                GroundSpeed = Convert.ToInt32(d.airspeed_true),
                FuelFlow = d.fuel_flow,
                VS = d.vspeed,
                SimTime = HelperService.SetZuluTime(d.local_time),
                ZuluTime = HelperService.SetZuluTime(d.zulu_time)
            };

            await _api.PostFlightLogAsync(log);
        }

        public async void EndFlight()
        {
            // check distance
            var distance = HelperService.CalculateDistance(Convert.ToDouble(txtArrLat.Text), Convert.ToDouble(txtArrLon.Text), endLat, endLon, true);
            if (distance > 2)
            {
                // get nearest airport and update pirep destination (return icao)
                var req = new NewLocationRequest
                {
                    Lat = endLat,
                    Lon = endLon,
                    PirepId = txtPirep.Text
                };

                try
                {
                    var newLocation = await _api.PostNewLocationAsync(req);

                    // update labels (destination icao)
                    txtArrLat.Text = endLat.ToString();
                    txtArrLon.Text = endLon.ToString();
                    txtArrival.Text = newLocation.Icao;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error finding alternate airport", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
                    lblErrorText.Text = ex.Message;
                }
            }

            var pirep = new Pirep()
            {
                PirepId = txtPirep.Text,
                FuelUsed = startFuelQty - endFuelQty,
                LandingRate = landingRate,
                TouchDownLat = landingLat,
                TouchDownLon = landingLon,
                TouchDownBank = landingBank,
                TouchDownPitch = landingPitch,
                BlockOffTime = startTime,
                BlockOnTime = endTime
            };
                        
            var res = await _api.PostPirepAsync(pirep);
            if (res)
            {
                MessageBox.Show("Pirep submitted!", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Information);
                TidyUpAfterPirepSubmission();
            } else
            {
                MessageBox.Show("Pirep Not Submitted!", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        public void SendTextToSim(string text)
        {
            simConnect.Text(SIMCONNECT_TEXT_TYPE.PRINT_BLACK, 5, SIMCONNECT_EVENT_FLAG.DEFAULT, text);
        }

#endregion

#region Form_Iteraction

        private void btnFetchBookings_Click(object sender, RoutedEventArgs e)
        {
            FetchDispatch();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            lblStart.Visibility = Visibility.Visible;
            btnStart.IsEnabled = false;
            if (flag)
            {
                MessageBox.Show("Your engine(s) must be off before starting", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                lblStart.Visibility = Visibility.Collapsed;
                btnStart.IsEnabled = true;
                return;
            }

            bFlightTracking = true;
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            EndFlight();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopTracking();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            OpenConnection();
            if (bConnected)
            {
                btnConnect.IsEnabled = false;
            }
        }

        private void btnEndFlight_Click(object sender, RoutedEventArgs e)
        {
            btnEndFlight.IsEnabled = false;
            lblEnd.Visibility = Visibility.Visible;
            bEndFlight = true;
        }

        #endregion


        #region Helper_methods

        protected void TidyUpAfterPirepSubmission()
        {
            btnStop.Visibility = Visibility.Hidden;
            
            ClearVariables();
            btnSubmit.IsEnabled = false;
            btnStart.Visibility = Visibility.Visible;
            btnEndFlight.IsEnabled = false;
            btnStart.IsEnabled = false;
            FetchDispatch();
        }

        private void ClearVariables()
        {
            bDispatch = false;
            bReady = false;
            bEndFlight = false;
            bFlightCompleted = false;
            bFirstData = true;
            bLastEngineStatus = false;
            startLat = 0;
            startLon = 0;
            endLat = 0;
            endLon = 0;
            startFuelQty = 0;
            endFuelQty = 0;
            startTime = "";
            endTime = "";
            flightStatus = Convert.ToInt32(PirepStatusType.BOARDING);
            lastHeading = 0;
            lastAltitude = 0;
            // stop simconnect tracking
            bFlightTracking = false;
        }

        private async void StopTracking()
        {
            ClearVariables();

            // reset pirep to draft and remove any logs
            var res = await _api.CancelTrackingAsync();
            if (res)
            {
                lblStatusText.Text = "Tracking Stopped";
                btnStop.Visibility = Visibility.Hidden;
                btnStart.Visibility = Visibility.Visible;
                btnStart.IsEnabled = true;
                btnEndFlight.IsEnabled = false;
            }
            else
            {
                lblErrorText.Text = "Issue cancelling pirep";
            }
        }

        private void SetDispatchData(Dispatch dispatch)
        {

            btnStart.IsEnabled = true;
            grpFlight.Visibility = Visibility.Visible;
            txtDeparture.Text = dispatch.Departure.ToString();
            txtArrival.Text = dispatch.Arrival.ToString();
            txtAircraft.Text = dispatch.Aircraft.ToString();
            txtAircraftType.Text = dispatch.AircraftType.ToString();
            txtRegistration.Text = dispatch.Registration.ToString();
            txtFuel.Text = dispatch.PlannedFuel.ToString();
            txtCargoWeight.Text = dispatch.CargoWeight.ToString();
            txtPaxCount.Text = dispatch.PassengerCount.ToString();
            txtPirep.Text = dispatch.Id.ToString();
            txtDepLat.Text = dispatch.DepLat.ToString();
            txtDepLon.Text = dispatch.DepLon.ToString();
            txtArrLat.Text = dispatch.ArrLat.ToString();
            txtArrLon.Text = dispatch.ArrLon.ToString();
            grpFlight.Visibility = Visibility.Visible;
        }

        public bool CheckReadyForStart(CheckStructure data)
        {
            // clear text errors
            ClearCheckErrors();
            var status = true;
            // check aircraft contains text in chosen aircraft
            //var test = data.Aircraft.Contains(txtAircraftType.Text);
            //if (!data.Aircraft.Contains(txtAircraftType.Text) || data.AircraftType != txtAircraftType.Text)
            //{
            //    // set error text for aircraft
            //    lblAircraftError.Content = "Aircraft does not match";
            //    lblAircraftError.Visibility = Visibility.Visible;
            //    status = false;
            //}
            // check fuel qty matches planned fuel
            var maxVal = Convert.ToDouble(txtFuel.Text) + 5;
            var minVal = Convert.ToDouble(txtFuel.Text) - 5;
            //var isFuelValid = Enumerable.Range(Convert.ToInt32(minVal), Convert.ToInt32(maxVal)).Contains(Convert.ToInt32(data.Fuel));
            //if (data.Fuel <= max && data.Fuel >= min)
            if (!(minVal <= data.Fuel) || !(data.Fuel <= maxVal))
            {
                // set error text for fuel
                lblFuelError.Content = "Fuel does not match";
                lblFuelError.Visibility = Visibility.Visible;
                status = false;
            }

            // TODO: check cargo weight matches
            //var w = Math.Round(data.Payload, 2).ToString();
            //if (txtCargoWeight.Text != Math.Floor(data.Payload).ToString())
            //{
            //    // set error text for payload
            //    lblCargoError.Content = "Cargo does not match";
            //    lblCargoError.Visibility = Visibility.Visible;
            //    status = false;
            //}

            // check current position
            var distance = HelperService.CalculateDistance(Convert.ToDouble(txtDepLat.Text), Convert.ToDouble(txtDepLon.Text), data.CurrentLat, data.CurrentLon, false);
            if (distance > 2)
            {
                // set error text for departure
                lblDepartureError.Content = "You are not at your planned departure";
                lblDepartureError.Visibility = Visibility.Visible;
                status = false;
            }
            return status;
        }

        public void ClearCheckErrors()
        {
            lblDepartureError.Visibility = Visibility.Hidden;
            lblAircraftError.Visibility = Visibility.Hidden;
            lblCargoError.Visibility = Visibility.Hidden;
            lblFuelError.Visibility = Visibility.Hidden;
        }

        public async void FetchDispatch()
        {
            lblStatusText.Text = "Ok";
            if (txtKey.Text == "")
            {
                MessageBox.Show("Please enter your API key", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (txtKey.Text != Properties.Settings.Default.Key)
            {
                Properties.Settings.Default.Key = txtKey.Text;
                Properties.Settings.Default.Save();
            }
            lblFetch.Visibility = Visibility.Visible;
            // make api request to get bookings/dispatch
            try
            {
                var dispatch = await _api.GetDispatchInfoAsync();
                var dispatchCargo = await _api.GetDispatchCargoAsync();
                // load bookings into grid
                dgBookings.ItemsSource = dispatchCargo;
                SetDispatchData(dispatch);
                bDispatch = true;
                if (bConnected)
                {
                    btnStart.IsEnabled = true;
                }
                else
                {
                    btnStart.IsEnabled = false;
                }
                lblFetch.Visibility = Visibility.Hidden;
                lblErrorText.Text = "";
            }
            catch (Exception ex)
            {
                lblFetch.Visibility = Visibility.Hidden;
                lblErrorText.Text = ex.Message;
                if (ex.Message == "Fetching dispatch info: No Content")
                {
                    dgBookings.ItemsSource = null;
                    grpFlight.Visibility = Visibility.Hidden;
                }
            }
        }
        #endregion
    }
}
