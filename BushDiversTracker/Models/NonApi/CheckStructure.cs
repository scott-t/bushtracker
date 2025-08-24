namespace BushDiversTracker.Models.NonApi
{
    public class CheckStructure
    {
        public SimSettingsData FlightSettings { get; set; }
        public double Fuel { get; set; }
        public double CurrentLat { get; set; }
        public double CurrentLon { get; set; }
        public bool CurrentEngineStatus { get; set; }
    }
}
