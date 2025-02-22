using System;
using BushDiversTracker.Models.NonApi;

namespace BushDiversTracker.Services
{
    interface ISimService
    {
        Models.Enums.SimVersion? Version { get; }
        bool IsConnected { get; }
        bool IsUserControlled { get; }

        event EventHandler OnSimConnected;
        event EventHandler OnSimDisconnected;
        event EventHandler<SimData> OnSimDataReceived;
        event EventHandler<SimLandingData> OnLandingDataReceived;

        void OpenConnection();
        void CloseConnection();
        void SendTextToSim(string text);
    }
}
