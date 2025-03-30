using BushDiversTracker.Models.Enums;
using BushDiversTracker.Models.NonApi;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using XPlaneConnector;


namespace BushDiversTracker.Services
{
    internal class SimServiceXPlane : ISimService
    {
        MainWindow _mainWindow;

        public event EventHandler OnSimConnected;
        public event EventHandler OnSimDisconnected;
        public event EventHandler<SimData> OnSimDataReceived;
        public event EventHandler<SimLandingData> OnLandingDataReceived;

        public SimVersion? Version { get => SimVersion.XPlane12; }

        private bool connectionActive = false;
        public bool IsConnected { get => _xplane != null && connectionActive; }
        public bool IsUserControlled { get => false; }

        XPlaneConnector.XPlaneConnector _xplane = null;
        protected readonly int FREQ = 1; // Seconds between polls

        SimData _simData = new();
        DateTime lastUpdate = new();
        Timer timer = new(5);

        #region StringDataRefs
        public StringDataRefElement AircraftViewAcfDescrip => new StringDataRefElement
        {
            DataRef = "sim/aircraft/view/acf_descrip", // acf_ui_name
            Frequency = FREQ
        };
        public DataRefElement FlightModel2PositionPressureAltitude => new DataRefElement
        {
            DataRef = "sim/flightmodel2/position/pressure_altitude",
            Units = "feet",
            Frequency = FREQ
        };
        public StringDataRefElement AircraftViewAcfType => new StringDataRefElement
        {
            DataRef = "sim/aircraft/view/acf_ICAO",
            Frequency = FREQ
        };
        #endregion

        public SimServiceXPlane(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            
        }

        public void OpenConnection()
        {
            if (_xplane != null)
                return;

            _xplane = new();

            _xplane.Subscribe(AircraftViewAcfDescrip, -1, (e, v) => _simData.title = v);
            // camera
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelPositionLatitude, -1, (e, v) => _simData.latitude = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelPositionLongitude, -1, (e, v) => _simData.longitude = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.Cockpit2GaugesIndicatorsAltitudeFtPilot, -1, (e, v) => _simData.indicated_altitude = v); // indicated
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelPositionElevation, -1, (e, v) => _simData.plane_altitude = v); // actual
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelPositionTheta, -1, (e, v) => _simData.ac_pitch = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelPositionPhi, -1, (e, v) => _simData.ac_bank = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelPositionVpath, -1, (e, v) => _simData.vspeed = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelPositionPsi, -1, (e, v) => _simData.heading_m = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelPositionMagPsi, -1, (e, v) => _simData.heading_t = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelForcesGNrml, -1, (e, v) => _simData.gforce = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelEngineENGNRunning, 0, (e, v) => _simData.eng1_combustion = (int)v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.EngineCombustion, 1, (e, v) => _simData.eng2_combustion = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.EngineCombustion, 2, (e, v) => _simData.eng3_combustion = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.EngineCombustion, 3, (e, v) => _simData.eng4_combustion = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.AircraftMaxRpm, -1, (e, v) => _simData.aircraft_max_rpm = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.MaxRpmAttained, -1, (e, v) => _simData.max_rpm_attained = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.TimeZuluTimeSec, -1, (e, v) => _simData.zulu_time = (int)v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.TimeLocalTimeSec, -1, (e, v) => _simData.local_time = (int)v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelFailuresOngroundAny, -1, (e, v) => _simData.on_ground = (int)v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelGroundSurfaceTextureType, -1, (e, v) => _simData.surface_type = (int)v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.AtcId, -1, (e, v) => _simData.atcId = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.AtcType, -1, (e, v) => _simData.atcType = v);
            _xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FlightmodelWeightMFuelTotal, -1, (e, v) => _simData.fuel_qty = v); //in kg, total fuel?
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FuelsystemTank1Capacity, -1, (e, v) => _simData.fuelsystem_tank1_capacity = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.UnusableFuelQty, -1, (e, v) => _simData.unusable_fuel_qty = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.IsOverspeed, -1, (e, v) => _simData.is_overspeed = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.IsUnlimited, -1, (e, v) => _simData.is_unlimited = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.PayloadStationCount, -1, (e, v) => _simData.payload_station_count = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.PayloadStationWeight, -1, (e, v) => _simData.payload_station_weight = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.MaxG, -1, (e, v) => _simData.max_g = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.MinG, -1, (e, v) => _simData.min_g = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.FuelFlow, -1, (e, v) => _simData.fuel_flow = v);
            _xplane.Subscribe(AircraftViewAcfType, -1, (e, v) => _simData.atcModel = v);
            //_xplane.Subscribe(XPlaneConnector.DataRefs.DataRefs.TotalWeight, -1, (e, v) => _simData.total_weight = v);



         
            _xplane.Start();
            timer.AutoReset = true;
            timer.Elapsed += (obj, args) =>
            {
                bool lastState = connectionActive;
                connectionActive = lastUpdate >= DateTime.Now - TimeSpan.FromSeconds(10);

                if (lastState != connectionActive)
                {
                    if (connectionActive)
                        OnSimConnected?.Invoke(this, EventArgs.Empty);
                    else
                        OnSimDisconnected?.Invoke(this, EventArgs.Empty);
                }
            };
            timer.Start();

        }

        public void CloseConnection()
        {
            _xplane.Stop();
            _xplane?.Dispose();
            _xplane = null;
        }

        public void SendTextToSim(string text)
        {
            
        }
    }
}
