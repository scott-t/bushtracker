using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BushDiversTracker.Services
{
    class HelperService
    {
        /// <summary>
        /// Calculates the distance between two points
        /// </summary>
        /// <param name="latFrom">double</param>
        /// <param name="lonFrom">double</param>
        /// <param name="latTo">double</param>
        /// <param name="lonTo">double</param>
        /// <param name="endRad">if end lat/ln is a radian already</param>
        /// <returns>
        /// Double of calculated distance
        /// </returns>
        public static double CalculateDistance(double latFrom, double lonFrom, double latTo, double lonTo, bool endRad = false)
        {
            latTo = DegToRad(latTo);
            lonTo = DegToRad(lonTo);

            latFrom = DegToRad(latFrom);
            lonFrom = DegToRad(lonFrom);

            double earthRadius = 3440.1;

            var thetaLat = latTo - latFrom;
            var thetaLon = lonTo - lonFrom;

            var a = Math.Sin(thetaLat / 2) * Math.Sin(thetaLat / 2) + Math.Cos(latFrom) * Math.Cos(latTo) * Math.Sin(thetaLon / 2) * Math.Sin(thetaLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = earthRadius * c;

            return d;
        }

        /// <summary>
        /// Converts deg lat/lon to radian
        /// </summary>
        /// <param name="deg">Double of degrees</param>
        /// <returns>double of radian</returns>
        public static double DegToRad(double deg)
        {
            double rad = (Math.PI / 180) * deg;
            return rad;
        }

        /// <summary>
        /// Takes time in seconds and converts to datetime
        /// </summary>
        /// <param name="zuluTimeInSecs">int seconds in time</param>
        /// <returns>DateTime of zulu</returns>
        public static DateTime SetZuluTime(int zuluTimeInSecs)
        {

            return DateTime.Today.Add(TimeSpan.FromSeconds((double)zuluTimeInSecs));
        }

        /// <summary>
        /// Checks if there has been a valid increase or decrease in altitude
        /// </summary>
        /// <param name="currentAlt">The current/previous alt</param>
        /// <param name="newAlt">The new alt</param>
        /// <returns>true if change is within zone</returns>
        public static bool CheckForAltChange(double currentAlt, double newAlt)
        {
            var increasedAlt = newAlt >= (currentAlt + 200) ? true : false;
            var decreasedAlt = newAlt <= (currentAlt - 200) ? true : false;
            if (increasedAlt || decreasedAlt) return true;
            else return false;
        }

        /// <summary>
        /// Checks if there has been a valid change in direction
        /// </summary>
        /// <param name="currentHdg">last heading</param>
        /// <param name="newHdg">new heading</param>
        /// <returns>true if heading change is 7 degrees or more</returns>
        public static bool CheckForHeadingChange(double currentHdg, double newHdg)
        {
            var left = currentHdg - newHdg;
            var right = newHdg - currentHdg;

            if (left < 0) left += 360;
            if (right < 0) right += 360;

            var headingChange = left < right ? left : right;

            if (headingChange >= 7) return true;
            else return false;
        }

        /// <summary>
        /// Sends information to log file
        /// </summary>
        /// <param name="msg">String to send to log file</param>
        public static void WriteToLog(string msg)
        {
            var fileName = DateTime.Now.Date.ToString();
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                w.WriteLine($"{msg}");
                w.WriteLine("-------------");
            }
        }

        /// <summary>
        /// Processes flight cancellation
        /// </summary>
        public static async void CancelFlightOnExit()
        {
            var _api = new APIService();
            await _api.CancelTrackingAsync();
        }

        public static decimal GalToLitre(decimal gal)
        {
            return gal * new decimal(3.785412);
        }

        public static decimal LbsToKG(decimal lbs)
        {
            return lbs * new decimal(0.453592);
        }
    }
}
