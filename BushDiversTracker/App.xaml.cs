using BushDiversTracker.Services;
using System.Windows;

namespace BushDiversTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            var message = "Are you sure you want to close Bush Tracker? If you have an active flight, progress will be lost";
            MessageBoxResult result;
            result = MessageBox.Show(message, "Bush Tracker", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }

            HelperService.CancelFlightOnExit();
        }
    }
}
