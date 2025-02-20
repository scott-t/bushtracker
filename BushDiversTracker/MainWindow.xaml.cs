using BushDiversTracker.Models;
using BushDiversTracker.Models.Enums;
using BushDiversTracker.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AutoUpdaterDotNET;
using System.Reflection;
using static BushDiversTracker.Services.TrackerService;
using System.Linq;
using System.Windows.Controls;

namespace BushDiversTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        APIService _api;
        AddonBrowser _addonBrowser;
        SimService _simConnect;
        TrackerService _tracker;

        internal enum MessageState
        {
            OK,
            Neutral,
            Error
        }

        public MainWindow()
        {
            InitializeComponent();

            HelperService.RotateLog();

            // Initialise visibility here to help UI editability
            lblErrorText.Visibility = Visibility.Hidden;
            grpFlight.Visibility = Visibility.Hidden;
            lblDeadHead.Visibility = Visibility.Hidden;

            txtPirep.Visibility = Visibility.Hidden;
            txtDepLat.Visibility = Visibility.Hidden;
            txtDepLon.Visibility = Visibility.Hidden;
            txtArrLat.Visibility = Visibility.Hidden;
            txtArrLon.Visibility = Visibility.Hidden;
            lblDepartureError.Visibility = Visibility.Hidden;
            lblCargoError.Visibility = Visibility.Hidden;
            lblAircraftError.Visibility = Visibility.Hidden;
            lblFuelError.Visibility = Visibility.Hidden;

            //btnStop.Visibility = Visibility.Hidden;
            lblFetch.Visibility = Visibility.Hidden;
            lblStart.Visibility = Visibility.Hidden;
            lblDistanceLabel.Visibility = Visibility.Hidden;
            lblDistance.Visibility = Visibility.Hidden;
            lblSubmitting.Visibility = Visibility.Hidden;

            btnStop.IsEnabled = false;

            txtKey.Password = Properties.Settings.Default.Key;
            _api = new APIService();
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                //AutoUpdater.Start("http://localhost/api/tracker-version");
                AutoUpdater.Start("https://fly.bushdivers.com/api/tracker-version");
            }

            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            lblVersion.Content = version;

            if (Properties.Settings.Default.UseMetric)
                rdoUnitMetric.IsChecked = true;
            else
                rdoUnitUS.IsChecked = true;

            _simConnect = new SimService(this);
            _simConnect.OnSimConnected += SimConnect_OnSimConnected;
            _simConnect.OnSimDisconnected += SimConnect_OnSimDisconnected;
            _simConnect.OnSimDataReceived += SimConnect_OnSimDataReceived;

            _tracker = new TrackerService(this, _simConnect, _api);
            _tracker.OnTrackerStateChanged += Tracker_OnStateChange;
            _tracker.OnDispatchError += Tracker_OnDispatchError;
            _tracker.OnSetDispatch += Tracker_SetDispatchData;
            _tracker.OnStatusMessage += (sender, message) => SetStatusMessage(message.Message, message.State);
        }

        #region SimConnect

        private void SimConnect_OnSimConnected(object sender, EventArgs e)
        {
            elConnection.Fill = Brushes.Green;
            elConnection.Stroke = Brushes.Green;
            SetStatusMessage("Connected");
        }

        private void SimConnect_OnSimDisconnected(object sender, EventArgs e)
        {
            elConnection.Fill = Brushes.Red;
            elConnection.Stroke = Brushes.Red;
            btnConnect.IsEnabled = true;
        }

        private void SimConnect_OnSimDataReceived(object sender, SimService.SimData data)
        {
            if (_tracker?.Dispatch != null)
                txtSimFuel.Text = FormatFuel(new decimal(data.fuel_qty), _tracker.Dispatch.FuelType);
            else
                txtSimFuel.Text = "";
        }


        #endregion

        #region Tracker UI interaction
        private void Tracker_OnStateChange(object sender, TrackerState state)
        {
            switch (state)
            {
                case TrackerState.None:
                    btnStop.IsEnabled = false;
                    btnSubmit.IsEnabled = true;
                    btnFetchBookings.IsEnabled = true;
                    lblDistance.Visibility = Visibility.Hidden;
                    lblDistanceLabel.Visibility = Visibility.Hidden;
                    btnSubmit.IsEnabled = false;
                    lblSubmitting.Visibility = Visibility.Hidden;
                    break;

                case TrackerState.HasDispatch:
                    btnStop.IsEnabled = true;
                    btnFetchBookings.IsEnabled = true;
                    break;

                case TrackerState.ReadyToStart:
                    lblDistance.Visibility = Visibility.Visible;
                    lblDistanceLabel.Visibility = Visibility.Visible;
                    btnStop.IsEnabled = true;
                    btnFetchBookings.IsEnabled = false;
                    break;

                case TrackerState.InFlight:
                    btnSubmit.IsEnabled = false;
                    break;

                case TrackerState.Shutdown:
                    btnSubmit.IsEnabled = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state));

            }
        }

        private void Tracker_OnDispatchError(object sender, TrackingStartErrorArgs status)
        {
            lblAircraftError.Visibility = status.AircraftError ? Visibility.Visible : Visibility.Hidden;
            lblCargoError.Visibility = status.CargoError ? Visibility.Visible : Visibility.Hidden;
            lblDepartureError.Visibility = status.DepartureError ? Visibility.Visible : Visibility.Hidden;
            lblFuelError.Visibility = status.FuelError ? Visibility.Visible : Visibility.Hidden;
        }

        #endregion

        #region Form_Iteraction

        private string FormatFuel(decimal fuel, FuelType? fuelType)
        {
            bool isMetric = rdoUnitMetric.IsChecked == true;
            string fuelString = isMetric ? HelperService.GalToLitre(fuel).ToString("0.## L") : fuel.ToString("0.## gal");
            if (fuelType.HasValue)
                fuelString += " | " + (isMetric ? HelperService.LbsToKG(HelperService.GalToLbs(fuel, fuelType.Value)).ToString("0.## kg") : HelperService.GalToLbs(fuel, fuelType.Value).ToString("0.## lbs"));

            return fuelString;
        }

        private async void btnFetchBookings_Click(object sender, RoutedEventArgs e)
        {
            await FetchDispatch();
        }


        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            lblSubmitting.Visibility = Visibility.Visible;
            btnSubmit.IsEnabled = false;
            lblErrorText.Visibility = Visibility.Hidden;
            await _tracker.SubmitFlight();

        }

        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("If you cancel you will need to restart your flight at a later time.\n\nAre you sure you wish to cancel your flight?", "Cancel?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _tracker.Stop();
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            btnConnect.IsEnabled = false;
            _simConnect.OpenConnection();
        }

        private void rdoUnitType_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UseMetric = rdoUnitMetric.IsChecked == true;
            Properties.Settings.Default.Save();

            UpdateDispatchWeight();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_addonBrowser != null && _addonBrowser.IsVisible)
            {
                _addonBrowser.Close();
                if (_addonBrowser != null)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (_tracker.State > TrackerState.HasDispatch)
            {
                if (_tracker.State == TrackerState.Shutdown || MessageBox.Show("A flight is currently in progress. You will need to restart your flight at a later time.\n\nAre you sure you wish to quit?", "Cancel current flight?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    e.Cancel = true;

                    await _tracker.Stop();
                    if (_tracker.State > TrackerState.HasDispatch || MessageBox.Show("There was an error cancelling your flight. If you continue you will need to cancel your dispatch via the web.\n\nQuit anyway?", "Quit?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        ((Window)sender).Close();
                    }
                }
                else
                    e.Cancel = true;
            }
            else if (_tracker.State == TrackerState.HasDispatch)
                await _tracker.Stop();

            if (!e.Cancel)
                _simConnect.CloseConnection();
        }

        #endregion


        #region Helper_methods

        
        /// <summary>
        /// Sets the dispatch information from server
        /// </summary>
        /// <param name="dispatch">Dispatch info to be set</param>
        private void Tracker_SetDispatchData(object sender, Dispatch dispatch)
        {
            if (dispatch == null)
            {
                grpFlight.Visibility = Visibility.Hidden;
                dgBookings.ItemsSource = null;
                return;
            }

            grpFlight.Visibility = Visibility.Visible;
            txtDeparture.Text = dispatch.Departure.ToString();
            txtArrival.Text = dispatch.Arrival.ToString();
            txtAircraft.Text = dispatch.Aircraft.ToString();
            txtAircraftType.Text = dispatch.AircraftType.ToString();
            txtRegistration.Text = dispatch.Registration.ToString();
            txtPirep.Text = dispatch.Id.ToString();
            txtDepLat.Text = dispatch.DepLat.ToString();
            txtDepLon.Text = dispatch.DepLon.ToString();
            txtArrLat.Text = dispatch.ArrLat.ToString();
            txtArrLon.Text = dispatch.ArrLon.ToString();
            string tourText  = dispatch.Tour != null ? dispatch.Tour.ToString() : "";
            txtTour.Text = tourText;
            UpdateDispatchWeight();
        }

        /// <summary>
        /// Update the display fields of weights and volumes based on user settings
        /// </summary>
        private void UpdateDispatchWeight()
        {
            if (_tracker?.Dispatch == null)
                return;

            bool isMetric = rdoUnitMetric.IsChecked == true;

            txtFuel.Text = FormatFuel(_tracker.Dispatch.PlannedFuel, _tracker.Dispatch.FuelType);
            txtCargoWeight.Text = isMetric ? HelperService.LbsToKG(_tracker.Dispatch.CargoWeight).ToString("0.# kg") : _tracker.Dispatch.CargoWeight.ToString("0 lbs");
            txtPaxCount.Text = _tracker.Dispatch.PassengerCount.ToString();
            if (_tracker.Dispatch.PassengerCount > 0)
            {
                decimal paxWeight = _tracker.Dispatch.PassengerCount * 170;
                var total = _tracker.Dispatch.CargoWeight + paxWeight;
                txtPayloadTotal.Text = isMetric ? HelperService.LbsToKG(total).ToString("0.# kg") : total.ToString("0 lbs");
            }
            else
            {
                txtPayloadTotal.Text = isMetric ? HelperService.LbsToKG(_tracker.Dispatch.CargoWeight).ToString("0.# kg") : _tracker.Dispatch.CargoWeight.ToString("0 lbs");
            }
        }
       

        /// <summary>
        /// Gets dispatch info from api
        /// </summary>
        public async Task FetchDispatch()
        {
            SetStatusMessage("Fetching dispatch");
            
            if (txtKey.Password == "")
            {
                MessageBox.Show("Please enter your API key", "Bush Tracker", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (txtKey.Password != Properties.Settings.Default.Key)
            {
                Properties.Settings.Default.Key = txtKey.Password;
                Properties.Settings.Default.Save();
            }

            lblFetch.Visibility = Visibility.Visible;
            // make api request to get bookings/dispatch

            if (_tracker.State >= TrackerState.HasDispatch)
            {
                if (_tracker.State == TrackerState.HasDispatch
                    || MessageBox.Show("A flight is currently in progress. Are you sure you wish to cancel the current tracking and fetch another dispatch?", "Cancel tracking?", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    await _tracker.Stop();

                }
            }

            try
            {
                var dispatch = await _api.GetDispatchInfoAsync();
                if (dispatch.IsEmpty == 0)
                {
                    var dispatchCargo = await _api.GetDispatchCargoAsync();
                    // load bookings into grid
                    dgBookings.ItemsSource = dispatchCargo;
                    dgBookings.Columns.Last().Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                }
                else
                {
                    lblDeadHead.Visibility = Visibility.Visible;
                }

                SetStatusMessage("Ok");

                _tracker.SetDispatchAndReset(dispatch);
            }
            catch (Exception ex)
            {
                SetStatusMessage(ex.Message, MessageState.Error);
                if (ex.Message == "Fetching dispatch info: No Content")
                {
                    dgBookings.ItemsSource = null;
                    grpFlight.Visibility = Visibility.Hidden;
                }
            }
            lblFetch.Visibility = Visibility.Hidden;
        }
        #endregion

        private void btnAddons_Click(object sender, RoutedEventArgs e)
        {
            if (_addonBrowser == null)
            {
                _addonBrowser = new AddonBrowser();
                _addonBrowser.Closed += delegate { _addonBrowser = null; };
                _addonBrowser.Show();
            }
            else
            {
                _addonBrowser.Focus();
            }
            
        }

        internal void SetStatusMessage(string message, MessageState state = MessageState.OK)
        {
            if (state == MessageState.Error)
            {
                lblStatusText.Visibility = Visibility.Hidden;
                lblErrorText.Text = message;
                lblErrorText.Visibility = Visibility.Visible;
            }
            else
            {
                lblErrorText.Visibility = Visibility.Hidden;
                lblStatusText.Text = message;
                lblStatusText.Visibility = Visibility.Visible;
            }
        }
    }
}
