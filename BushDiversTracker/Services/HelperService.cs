using BushDiversTracker.Models.Enums;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BushDiversTracker.Services
{
    class HelperService
    {
        // Constants for conversion factors
        private const decimal GALLONS_TO_LITRES = 3.785412m;
        private const decimal AVGAS_DENSITY_LBS_PER_GALLON = 6.00m;
        private const decimal JET_FUEL_DENSITY_LBS_PER_GALLON = 6.79m;
        private const decimal LBS_TO_KG = 0.453592m;
        private const double NAUTICAL_MILES_EARTH_RADIUS = 3440.1;
        private const double ALTITUDE_CHANGE_THRESHOLD = 200.0;
        private const double HEADING_CHANGE_THRESHOLD = 7.0;
        private const long MAX_LOG_SIZE_BYTES = 512 * 1024; // 512 KB

        protected static string BasePath { get; set; }

        /// <summary>
        /// Convert to/from string and Version class
        /// </summary>
        internal class VersionJsonConverter : System.Text.Json.Serialization.JsonConverter<Version>
        {
            public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string ver = reader.GetString();
                try 
                {
                    return new Version(ver);
                }
                catch { }

                if (ver.Contains('-'))
                {
                    ver = ver[..ver.IndexOf('-')];
                    try
                    {
                        return new Version(ver);
                    }
                    catch { }
                }
                return new Version();
            }
            public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }
        internal class StringBoolJsonConverter : System.Text.Json.Serialization.JsonConverter<Boolean>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        var tok = reader.GetString().ToLower();
                        return !(tok == "false" || tok == "0");

                    case JsonTokenType.Number:
                        return reader.GetInt32() != 0;

                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        return reader.GetBoolean();

                    default:
                        throw new NotSupportedException();
                }
            }
            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(Convert.ToString(value));
            }
        }

        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Converters =
            {
                new VersionJsonConverter(),
                new StringBoolJsonConverter()
            }
        };

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

            var thetaLat = latTo - latFrom;
            var thetaLon = lonTo - lonFrom;

            var a = Math.Sin(thetaLat / 2) * Math.Sin(thetaLat / 2) + Math.Cos(latFrom) * Math.Cos(latTo) * Math.Sin(thetaLon / 2) * Math.Sin(thetaLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = NAUTICAL_MILES_EARTH_RADIUS * c;

            return d;
        }

        /// <summary>
        /// Converts deg lat/lon to radian
        /// </summary>
        /// <param name="deg">Double of degrees</param>
        /// <returns>double of radian</returns>
        public static double DegToRad(double deg)
        {
            return (Math.PI / 180) * deg;
        }

        /// <summary>
        /// Takes time in seconds and converts to datetime
        /// </summary>
        /// <param name="zuluTimeInSecs">int seconds in time</param>
        /// <returns>DateTime of zulu</returns>
        public static DateTime SetZuluTime(int zuluTimeInSecs)
        {
            return DateTime.Today.Add(TimeSpan.FromSeconds(zuluTimeInSecs));
        }

        /// <summary>
        /// Checks if there has been a valid increase or decrease in altitude
        /// </summary>
        /// <param name="currentAlt">The current/previous alt</param>
        /// <param name="newAlt">The new alt</param>
        /// <returns>true if change is within zone</returns>
        public static bool CheckForAltChange(double currentAlt, double newAlt)
        {
            return Math.Abs(newAlt - currentAlt) >= ALTITUDE_CHANGE_THRESHOLD;
        }

        /// <summary>
        /// Checks if there has been a valid change in direction
        /// </summary>
        /// <param name="currentHdg">last heading</param>
        /// <param name="newHdg">new heading</param>
        /// <returns>true if heading change is 7 degrees or more</returns>
        public static bool CheckForHeadingChange(double currentHdg, double newHdg)
        {
            var delta = Math.Abs(newHdg - currentHdg);

            // Find the shortest angular distance
            if (delta > 180)
                delta = 360 - delta;

            return delta >= HEADING_CHANGE_THRESHOLD;
        }

        private static readonly object _logLock = new();

        /// <summary>
        /// Sends information to log file
        /// </summary>
        /// <param name="msg">String to send to log file</param>
        public static void WriteToLog(string msg)
        {
            try
            {
                lock (_logLock)
                {
                    File.AppendAllText("log.txt", $"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}: {msg}\n");
                }
            }
            catch (Exception)
            {
                //
            }
        }

        /// <summary>
        /// Rotates the log file if too large
        /// </summary>
        public static void RotateLog()
        {
            FileInfo f = new("log.txt");
            if (f.Exists && f.Length > MAX_LOG_SIZE_BYTES)
            {
                try
                {
                    if (File.Exists("log.1.txt"))
                        File.Delete("log.1.txt");

                    f.MoveTo("log.1.txt");
                }
                catch (Exception)
                {
                    //
                }
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

        /// <summary>
        /// Convert gallons to litres
        /// </summary>
        /// <param name="gal">Gallons</param>
        /// <returns>Litres</returns>
        public static decimal GalToLitre(decimal gal)
        {
            return gal * GALLONS_TO_LITRES;
        }

        /// <summary>
        /// Convert fuel gallons to pounds
        /// </summary>
        /// <param name="gal">Gallons</param>
        /// <param name="fuel">Fuel type</param>
        /// <returns></returns>
        public static decimal GalToLbs(decimal gal, FuelType fuel)
        {
            return fuel switch
            {
                FuelType.AVGAS => gal * AVGAS_DENSITY_LBS_PER_GALLON,
                FuelType.JET => gal * JET_FUEL_DENSITY_LBS_PER_GALLON,
                _ => 0m
            };
        }

        /// <summary>
        /// Convert pounds to kilograms
        /// </summary>
        /// <param name="lbs">Pounds</param>
        /// <returns>Kilograms</returns>
        public static decimal LbsToKG(decimal lbs)
        {
            return lbs * LBS_TO_KG;
        }

        /// <summary>
        /// Find the local community package path
        /// </summary>
        /// <returns></returns>
        public static string GetPackagePath()
        {
            string path = Properties.Settings.Default.CommunityDir;

            if (Directory.Exists(path))
                return path;

            path = GetBasePath() + "\\Community";

            if (!Directory.Exists(path))
                return null; // Give up

            Properties.Settings.Default.CommunityDir = path;
            Properties.Settings.Default.Save();

            return path;
        }

        /// <summary>
        /// Find the local official package path
        /// </summary>
        /// <returns></returns>
        public static string GetOfficialPath()
        {
            string path = GetBasePath() + "\\Official";

            if (Directory.Exists(path + "\\Steam"))
            {
                path += "\\Steam";
            }
            else if (Directory.Exists(path + "\\OneStore"))
            {
                path += "\\OneStore";
            }
            else
            {
                var dirs = Directory.GetDirectories(path);
                if (dirs.Length == 1)
                    path = dirs[0];
            }

            return path;
        }

        /// <summary>
        /// Find the base FS package path
        /// </summary>
        /// <returns></returns>
        public static string GetBasePath()
        {
            if (BasePath?.Length > 0 && Directory.Exists(BasePath))
                return BasePath;

            // Go searching
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft Flight Simulator\\UserCfg.opt";
            if (!File.Exists(path))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Packages\\Microsoft.FlightSimulator_8wekyb3d8bbwe\\LocalCache\\UserCfg.opt";
                if (!File.Exists(path))
                    return null; // Couldn't find on Steam or Store
            }

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                return null;

            path = lines.FirstOrDefault(line => line.StartsWith("InstalledPackagesPath "));
            if (path.Length == 0)
                return null;

            path = path[23..^1];
            if (!Directory.Exists(path))
                return null;

            BasePath = path;

            return BasePath;
        }
    }
}
