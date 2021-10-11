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

        public static double DegToRad(double deg)
        {
            double rad = (Math.PI / 180) * deg;
            return rad;
        }

        public static DateTime SetZuluTime(int zuluTimeInSecs)
        {

            return DateTime.Today.Add(TimeSpan.FromSeconds((double)zuluTimeInSecs));
        }

        public static bool CheckForAltChange(double currentAlt, double newAlt)
        {
            var increasedAlt = newAlt >= (currentAlt + 200) ? true : false;
            var decreasedAlt = newAlt <= (currentAlt - 200) ? true : false;
            if (increasedAlt || decreasedAlt) return true;
            else return false;
        }

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
    }
}
