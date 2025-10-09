using System;
using BushDiversTracker.Models.NonApi;

namespace BushDiversTracker.Services
{
    interface ISimService
    {
        Models.Enums.SimVersion? Version { get; }
        bool IsConnected { get; }
        bool IsUserControlled { get; }
        bool SendSimText { get; set; }

        event EventHandler OnSimConnected;
        event EventHandler OnSimDisconnected;
        event EventHandler<SimData> OnSimDataReceived;
        event EventHandler<SimLandingData> OnLandingDataReceived;
        event EventHandler<SimSettingsData> OnFlightSettingsReceived;

        void OpenConnection();
        void CloseConnection();
        void SendTextToSim(string text);

        /// <summary>
        /// Set strict mode for flight tracking (disable slew, etc)
        /// </summary>
        /// <param name="strictMode"></param>
        void SetStrictMode(bool strictMode, double dispatchWeight);
    }
}
