using BushDiversTracker.Models.Enums;
using BushDiversTracker.Models.NonApi;
using BushDiversTracker.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
using BushDiversTracker.Properties;

namespace BushDiversTracker.Services
{
    internal class TrackerService
    {
        // Define events that other classes can subscribe to
        public event EventHandler<MessageEventArgs> OnStatusMessage;
        public struct MessageEventArgs
        {
            public string Message { get; set; }
            public MainWindow.MessageState State { get; set; }
        }

        public event EventHandler<TrackingStartErrorArgs> OnDispatchError;
        public struct TrackingStartErrorArgs
        {
            public bool DepartureError { get; init; }
            public bool AircraftError { get; init; }
            public bool FuelError { get; init; }
            public bool CargoError { get; init; }
        }

        public event EventHandler<TrackerState> OnTrackerStateChanged;
        public enum TrackerState
        {
            None,
            HasDispatch,
            ReadyToStart,
            InFlight,
            Shutdown,
        }

        public event EventHandler<Dispatch> OnSetDispatch;

        private TrackerState state = TrackerState.None;
        public TrackerState State { get => state; }
        public bool AllowStart = Settings.Default.AutoStart;
        public bool AllowEngineHotstart = false;

        // Bush Tracker variables
        private Dispatch dispatchData = null;
        public Dispatch Dispatch { get => dispatchData; }

        private bool bEnginesRunning = false;
        private double lastLat;
        private double lastLon;
        private double currentDistance = 0;
        private double startFuelQty;
        private double endFuelQty;
        private string startTime;
        private string endTime;
        private bool engineHotstart;
        private PirepStatusType flightStatus;
        public PirepStatusType FlightStatus { get => flightStatus; private set {
                if (flightStatus == value)
                    return;

                flightStatus = value;
                OnFlightStatusChanged?.Invoke(this, flightStatus);
            }
        }
        public event EventHandler<PirepStatusType> OnFlightStatusChanged;

        protected double lastHeading;
        protected double lastAltitude;
        protected double landingRate;
        protected double landingBank;
        protected double landingGforce;
        protected double landingPitch;
        protected double landingLat;
        protected double landingLon;
        protected double lastVs;
        protected double lastGforce;
        protected bool lastOnground;
        protected DateTime dataLastSent;
        protected string aircraftName;

        private readonly MainWindow _mainWindow = null;
        private readonly APIService _api = null;
        private ISimService _sim = null;

        public TrackerService(MainWindow mainWindow, ISimService simService, APIService api)
        {
            _mainWindow = mainWindow;
            _api = api;
            SetSimService(simService);
        }

        public async void SetSimService(ISimService simService)
        {
            await Stop();

            _sim = simService;
            if (_sim != null)
            {
                simService.OnSimDataReceived += SimService_OnSimDataReceived;
                simService.OnLandingDataReceived += SimService_OnLandingDataReceived;
            }
        }

        public void Start()
        {
            // Start tracking
        }

        public async Task<bool> Stop()
        {
            // Stop tracking
            try
            {
                if (dispatchData != null)
                {
                    if (state == TrackerState.Shutdown)
                    {
                        if (!await SubmitFlight())
                            throw new Exception("Error submitting flight");
                    }
                    else if (!await _api.CancelTrackingAsync())
                        throw new Exception("Error resetting on server");

                    _mainWindow.SetStatusMessage(state == TrackerState.Shutdown ? "Dispatch submitted" : "Ok");
                    SetDispatchAndReset(null);   
                }
                return true;
            }
            catch (Exception ex)
            {
                _mainWindow.SetStatusMessage("Error cancelling tracking: " + ex.Message, MainWindow.MessageState.Error);
            }

            return false;
        }

        /// <summary>
        /// Check for diversion. This does _not_ check if the aircraft is on the ground or active
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CheckAndDivert()
        {
            double distance = HelperService.CalculateDistance(Convert.ToDouble(dispatchData.ArrLat), Convert.ToDouble(dispatchData.ArrLon), lastLat, lastLon, true);
            if (distance <= 2)
                return true;

            // get nearest airport and update pirep destination (return icao)
            NewLocationRequest req = new()
            {
                Lat = lastLat,
                Lon = lastLon,
                PirepId = dispatchData.Id
            };

            try
            {
                NewLocationResponse newLocation = await _api.PostNewLocationAsync(req);

                dispatchData.ArrLat = newLocation.Lat;
                dispatchData.ArrLon = newLocation.Lon;
                dispatchData.Arrival = newLocation.Icao;
                OnSetDispatch?.Invoke(this, dispatchData);
            }
            catch (Exception)
            {
                //_mainWindow.SetStatusMessage(e.Message, MainWindow.MessageState.Error);
                _mainWindow.SetStatusMessage("No airport within 2NM", MainWindow.MessageState.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Ends a flight and submits the pirep
        /// </summary>
        public async Task<bool> SubmitFlight()
        {
            // check distance
            if (bEnginesRunning && _sim.IsConnected)
                return false;

            if (!await CheckAndDivert())
            {
                MessageBox.Show("Unable to find landing airport.\n\nPlease resume flying and land within 2NM of an airport", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
                _mainWindow.SetStatusMessage("No airport within 2NM", MainWindow.MessageState.Error);
                return false;
            }

            Pirep pirep = new()
            {
                PirepId = dispatchData.Id,
                FuelUsed = startFuelQty - endFuelQty,
                LandingRate = landingRate,
                TouchDownLat = landingLat,
                TouchDownLon = landingLon,
                TouchDownBank = landingBank,
                TouchDownPitch = landingPitch,
                BlockOffTime = startTime,
                BlockOnTime = endTime,
                Distance = currentDistance,
                AircraftUsed = aircraftName,
                SimUsed = _sim.Version.Value.ToString(),
                EngineHotStart = engineHotstart
            };

            bool res = false;
            try
            {
                res = await _api.PostPirepAsync(pirep);
            }
            catch (Exception)
            {
                _mainWindow.SetStatusMessage("Error submitting pirep", MainWindow.MessageState.Error);
            }

            if (res)
            {
                _mainWindow.SetStatusMessage("Pirep submitted", MainWindow.MessageState.OK);
                SetDispatchAndReset(null);
            }
            else
            {
                MessageBox.Show("Pirep Not Submitted!", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return res;
        }


        /// <summary>
        /// Set new dispatch data and reset tracker for a new flight
        /// </summary>
        /// <param name="dispatch"></param>
        internal void SetDispatchAndReset(Dispatch dispatch)
        {
            // Set dispatch data
//            if (state == TrackerState.None)
            {
                dispatchData = dispatch;

                SetTrackerState(dispatch != null ? TrackerState.HasDispatch : TrackerState.None);

                lastLat = 0.0;
                lastLon = 0.0;
                currentDistance = 0.0;
                startFuelQty = 0.0;
                endFuelQty = 0.0;
                startTime = "";
                endTime = "";
                FlightStatus = dispatchData != null ? PirepStatusType.BOARDING : PirepStatusType.PREFLIGHT;
                lastHeading = 0;
                lastAltitude = 0;
                engineHotstart = false;

                // Landing vars not reset here, the tracker state logic does it when setting ready to start

                if (dispatchData != null)
                    _ = _api.PostPirepStatusAsync(new PirepStatus { PirepId = dispatchData.Id, Status = (int)PirepStatusType.BOARDING });

                OnSetDispatch?.Invoke(this, dispatch);
            }
        }

        private void SetTrackerState(TrackerState newState)
        {
            state = newState;
            OnTrackerStateChanged?.Invoke(this, state);
        }

        private async void SimService_OnSimDataReceived(object sender, SimData data)
        {
            // If we have no dispatch data, ignore
            if (state == TrackerState.None || dispatchData == null)
                return;

            // Unlimited fuel check (at least until flight starts *cough*)
            if (state < TrackerState.InFlight)
            {
                if (data.is_unlimited != 0)
                {
                    OnStatusMessage?.Invoke(this, new MessageEventArgs { Message = "Please turn off unlimited fuel", State = MainWindow.MessageState.Error });
                    return;
                }
            }

            bEnginesRunning = data.eng1_combustion > 0 || data.eng2_combustion > 0 || data.eng3_combustion > 0 || data.eng4_combustion > 0;

            if (state == TrackerState.HasDispatch)
            {
                if (CheckReadyForStart(new CheckStructure
                {
                    Aircraft = data.title,
                    AircraftType = data.atcType,
                    Fuel = data.fuel_qty,
                    Payload = data.total_weight,
                    Pax = 0,
                    CurrentLat = data.latitude,
                    CurrentLon = data.longitude,
                    CurrentEngineStatus = bEnginesRunning && !AllowEngineHotstart
                }))
                {
                    if (!AllowStart)
                    {
                        _mainWindow.SetStatusMessage("Waiting for start checkbox", MainWindow.MessageState.Neutral);
                    }
                    else
                    {
                        engineHotstart = bEnginesRunning;
                        SetTrackerState(TrackerState.ReadyToStart);
                        _mainWindow.SetStatusMessage("Pre-flight|Loading");
                        _sim.SendTextToSim("Bush Tracker Status: Pre-Flight - Ready");

                        // Clear landing rate so next change event as per simconnect is viewed as 'new'
                        landingRate = 0.0;
                    }
                }
            }
            else if (state == TrackerState.ReadyToStart)
            {
                // Once engines started, set block time and start location
                if (bEnginesRunning && Convert.ToBoolean(data.on_ground))
                {
                    startTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    startFuelQty = data.fuel_qty;
                    FlightStatus = PirepStatusType.BOARDING;
                    SetTrackerState(TrackerState.InFlight);

                    _ = _api.PostPirepStatusAsync(new PirepStatus { PirepId = dispatchData.Id, Status = (int)PirepStatusType.BOARDING });                
                }
            }
            else if (state == TrackerState.InFlight)
            {
                bool onGround = Convert.ToBoolean(data.on_ground);
                // check for take off
                if (FlightStatus == PirepStatusType.BOARDING && !onGround && data.plane_altitude > 200) // arbitrary number to avoid advancing state on bouncy water takeoff
                {
                    FlightStatus = PirepStatusType.DEPARTED;
                    _ = _api.PostPirepStatusAsync(new PirepStatus { PirepId = dispatchData.Id, Status = (int)PirepStatusType.DEPARTED });

                    _mainWindow.SetStatusMessage("Departed");
                    _sim.SendTextToSim("Bush Tracker Status: Departed - Have a good flight!");
                }
                else if (FlightStatus == PirepStatusType.DEPARTED && data.plane_altitude > 1000)
                {
                    FlightStatus = PirepStatusType.CRUISE;
                    _ = _api.PostPirepStatusAsync(new PirepStatus { PirepId = dispatchData.Id, Status = (int)PirepStatusType.CRUISE });
                    _mainWindow.SetStatusMessage("Cruise");
                }
                else if (FlightStatus == PirepStatusType.CRUISE)
                {
                    if (onGround && data.airspeed_true < 25)
                    {
                        FlightStatus = PirepStatusType.LANDED;
                        _ = _api.PostPirepStatusAsync(new PirepStatus { PirepId = dispatchData.Id, Status = (int)PirepStatusType.LANDED });

                        _mainWindow.SetStatusMessage("Landed");
                        _sim.SendTextToSim("Bush Tracker Status: Landed");
                    }

                    if (onGround && !lastOnground && data.surface_type == 2) // landed on water
                    {
                        var rate = -(data.vspeed + lastVs) * 60.0 / 2.0;    // f/s to f/m... api needs f/s on the pirep progress log

                        // In case of 'bounce' between the two refs
                        if (rate < 0.0)
                            rate = 0.0;

                        if (landingRate < rate || (landingLat == 0.0 && landingLon == 0.0))
                        {
                            landingRate = rate;
                            landingPitch = data.ac_pitch;
                            landingBank = data.ac_bank;
                            landingLat = data.latitude;
                            landingLon = data.longitude;
                        }
                    }
                   
                }
                else if (FlightStatus == PirepStatusType.LANDED)
                {
                    if (!onGround && data.plane_altitude > 50)
                    {
                        FlightStatus = PirepStatusType.CRUISE;
                        _ = _api.PostPirepStatusAsync(new PirepStatus { PirepId = dispatchData.Id, Status = (int)PirepStatusType.CRUISE });
                        _mainWindow.SetStatusMessage("Cruise");
                    }
                    else if (!bEnginesRunning && data.airspeed_true < 25)
                    {
                        FlightStatus = PirepStatusType.ARRIVED;
                        SetTrackerState(TrackerState.Shutdown);
                        
                        _ = _api.PostPirepStatusAsync(new PirepStatus { PirepId = dispatchData.Id, Status = (int)PirepStatusType.ARRIVED });

                        _mainWindow.SetStatusMessage("Flight ended");
                        _sim.SendTextToSim("Bush Tracker Status: Flight ended - Thanks for working with Bush Divers");

                        endFuelQty = data.fuel_qty;
                        // endTime = HelperService.SetZuluTime(data1.zulu_time).ToString("yyyy-MM-dd HH:mm:ss");
                        endTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                        aircraftName = data.title;

                        _ = CheckAndDivert();
                    }
                }
            }
            else if (state == TrackerState.Shutdown)
            {
                // If engines start again, go back to in-flight

                if (bEnginesRunning)
                {
                    SetTrackerState(TrackerState.InFlight);
                    FlightStatus = PirepStatusType.LANDED;
                    _mainWindow.SetStatusMessage("Landed");
                    _ = _api.PostPirepStatusAsync(new PirepStatus { PirepId = dispatchData.Id, Status = (int)PirepStatusType.LANDED });
                }
            }

            if (state >= TrackerState.ReadyToStart)
            {
                if (lastLat != 0.0 || lastLon != 0.0)
                {
                    // calc distance
                    var d = HelperService.CalculateDistance(lastLat, lastLon, data.latitude, data.longitude);
                    if (d > 50 
                        || (_sim.Version == SimVersion.FS2024 
                            && (data.camera_state == SimServiceMSFS.CameraState.FS2024.WORLD_MAP || data.camera_state == SimServiceMSFS.CameraState.FS2024.MAIN_MENU)
                            )
                        )
                    {
                        if (state == TrackerState.Shutdown)
                        {
                            state = TrackerState.None;
                            await SubmitFlight();
                        }
                        else
                        {
                            Task<bool> task = _api.CancelTrackingAsync();

                            SetTrackerState(TrackerState.None);
                            MessageBox.Show("It looks like you have abandoned your flight, tracking will now stop and your progress cancelled." + "\n" + "You can start your flight again by returning to the departure location", "Bush Divers", MessageBoxButton.OK);

                            task.Wait();
                            if (task.IsCompletedSuccessfully && task.Result)
                            {
                                _mainWindow.SetStatusMessage("Tracking stopped");
                                SetDispatchAndReset(null);
                            }
                        }

                        return;
                    }
                    currentDistance += d;
                    _mainWindow.lblDistance.Content = currentDistance.ToString("0.## nm"); // TODO: event?
                }
                else
                    currentDistance = 0.0;
         
                lastLat = data.latitude;
                lastLon = data.longitude;

                // Send data to api
                var headingChanged = HelperService.CheckForHeadingChange(lastHeading, data.heading_m);
                var altChanged = HelperService.CheckForAltChange(lastAltitude, data.indicated_altitude);
                // determine if data has changed or not
                if (headingChanged || altChanged || DateTime.UtcNow > dataLastSent.AddSeconds(60))
                {
                    _ = SendFlightLog(data);
                    dataLastSent = DateTime.UtcNow;
                }

                lastAltitude = data.indicated_altitude;
                lastHeading = data.heading_m;
                lastVs = data.vspeed;
                lastGforce = data.gforce;
                lastOnground = Convert.ToBoolean(data.on_ground);

            }

            // set reg number
            // simConnect.SetDataOnSimObject(SET_DATA.ATC_ID, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, txtRegistration.Text);
        }

        private void SimService_OnLandingDataReceived(object sender, SimLandingData data)
        {
            if (landingRate < data.touchdown_velocity)
            {
                landingRate = data.touchdown_velocity;
                landingPitch = data.touchdown_pitch;
                landingBank = data.touchdown_bank;
                landingLat = data.touchdown_lat;
                landingLon = data.touchdown_lon;
            }

            HelperService.WriteToLog("Landing data received: " + data.touchdown_velocity.ToString("0.##") + "fpm " + data.touchdown_pitch.ToString("0.##") + "deg / " + data.touchdown_bank.ToString("0.##") + "deg at " + data.touchdown_lat.ToString("0.##") + " " + data.touchdown_lon.ToString("0.##"));
        }


        /// <summary>
        /// Runs checks to make sure in a ready state to start flight
        /// </summary>
        /// <param name="data">data to check if flight is read to start</param>
        /// <returns>
        /// True if ready, false if something is not setup correctly
        /// </returns>
        public bool CheckReadyForStart(CheckStructure data)
        {
            if (dispatchData == null)
            {
                OnDispatchError?.Invoke(this, new TrackingStartErrorArgs());
                return false;
            }

            bool aircraftError = false;
            bool fuelError = false;
            bool cargoError = false;
            bool departureError = false;

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
            var tolerance = decimal.ToDouble(dispatchData.PlannedFuel) * .01;
            if (tolerance < 5.0)
                tolerance = 5;
            var maxVal = decimal.ToDouble(dispatchData.PlannedFuel) + tolerance;
            var minVal = decimal.ToDouble(dispatchData.PlannedFuel) - tolerance;
            if (data.Fuel < minVal || data.Fuel > maxVal)
                fuelError = true;

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
            var distance = HelperService.CalculateDistance(decimal.ToDouble(dispatchData.DepLat), decimal.ToDouble(dispatchData.DepLon), data.CurrentLat, data.CurrentLon, false);
            if (distance > 2)
                departureError = true;

            OnDispatchError?.Invoke(this, new TrackingStartErrorArgs { AircraftError = aircraftError, FuelError = fuelError, CargoError = cargoError, DepartureError = departureError });

            bool isInSim = _sim.IsUserControlled;
            bool readyToStart = !aircraftError
                && !fuelError
                && !cargoError
                && !departureError;

            if (!isInSim)
                OnStatusMessage?.Invoke(this, new MessageEventArgs { Message = "Waiting for world to load", State = MainWindow.MessageState.Error });
            else if (!readyToStart)
                OnStatusMessage?.Invoke(this, new MessageEventArgs { Message = "Start conditions not met", State = MainWindow.MessageState.Error });
            else if (data.CurrentEngineStatus)
                OnStatusMessage?.Invoke(this, new MessageEventArgs { Message = "Shutdown engines before starting", State = MainWindow.MessageState.Error });
            else
                OnStatusMessage?.Invoke(this, new MessageEventArgs { Message = "Ready to start", State = MainWindow.MessageState.OK });

            return readyToStart && !data.CurrentEngineStatus && isInSim;
        }

        /// <summary>
        /// Sends a flight log to the api
        /// </summary>
        /// <param name="d">data from simconnect</param>
        public async Task SendFlightLog(SimData d)
        {
            var log = new FlightLog()
            {
                PirepId = dispatchData.Id,
                Lat = d.latitude,
                Lon = d.longitude,
                Heading = Convert.ToInt32(d.heading_m),
                Altitude = Convert.ToInt32(d.indicated_altitude),
                IndicatedSpeed = Convert.ToInt32(d.airspeed_indicated),
                GroundSpeed = Convert.ToInt32(d.airspeed_true),
                FuelFlow = d.fuel_flow,
                VS = d.vspeed,
                SimTime = HelperService.SetZuluTime(d.local_time),
                ZuluTime = HelperService.SetZuluTime(d.zulu_time),
                Distance = currentDistance
            };

            try
            {
                await _api.PostFlightLogAsync(log);
                _mainWindow.SetStatusMessage("Flight log updated", MainWindow.MessageState.OK);
            }
            catch (Exception e)
            {
                _mainWindow.SetStatusMessage("Error submitting flight update: " + e.Message, MainWindow.MessageState.Error);
            }
        }
    }
}
