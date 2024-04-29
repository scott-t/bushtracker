using BushDiversTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BushDiversTracker.Services
{
    class APIService
    {
        protected string baseUrl = "https://fly.bushdivers.com/api";
        // protected string baseUrl = "http://localhost:8000/api";
        HttpClient _http = new HttpClient();

        public APIService()
        {
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var thisAssembly = System.Windows.Application.ResourceAssembly.GetName();
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(thisAssembly.Name, thisAssembly.Version.ToString()));
        }

        /// <summary>
        /// Gets dispatch cargo/pax data
        /// </summary>
        /// <returns>Collection of cargo</returns>
        public async Task<ICollection<DispatchCargo>> GetDispatchCargoAsync()
        {
            HttpResponseMessage res = await _http.GetAsync($"{baseUrl}/dispatch/cargo");
            if (res.StatusCode == HttpStatusCode.OK)
            {
                return await res.Content.ReadFromJsonAsync<ICollection<DispatchCargo>>();
            }
            else if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var msg = "Unauthorised";
                throw new Exception(msg);
            }
            else
            {
                string msg = await res.Content.ReadAsStringAsync();
                HelperService.WriteToLog($"Fetching dispatch cargo: {res.ReasonPhrase}");
                throw new Exception($"Fetching dispatch cargo: {res.ReasonPhrase}");
            }
        }

        /// <summary>
        /// Gets dispatch info from api
        /// </summary>
        /// <returns>Dispatch information</returns>
        public async Task<Dispatch> GetDispatchInfoAsync()
        {
            // First 'dispatch' call should update auth details
            AddAuthHeaders();

            HttpResponseMessage res = await _http.GetAsync($"{baseUrl}/dispatch");
            if (res.StatusCode == HttpStatusCode.OK)
            {
                return await res.Content.ReadFromJsonAsync<Dispatch>();
            }
            else if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var msg = "Unauthorised";
                // Basic check
                if (!System.Text.RegularExpressions.Regex.IsMatch(Properties.Settings.Default.Key, @"^\d+\|.{40}.*$"))
                    msg += " - API key does not match expected format";
                throw new Exception(msg);
            }
            else
            {
                string msg = await res.Content.ReadAsStringAsync();
                HelperService.WriteToLog($"Fetching dispatch info: {res.ReasonPhrase}");
                throw new Exception($"Fetching dispatch info: {res.ReasonPhrase}");
            }
        }

        /// <summary>
        /// Sends new arrival location to api, if different from intended destination
        /// </summary>
        /// <param name="newLocation">New location details</param>
        /// <returns>New location feedback</returns>
        public async Task<NewLocationResponse> PostNewLocationAsync(NewLocationRequest newLocation)
        {
            HttpResponseMessage res = await _http.PostAsJsonAsync($"{baseUrl}/pirep/destination", newLocation);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                return await res.Content.ReadFromJsonAsync<NewLocationResponse>();
            }
            else if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var msg = "Unauthorised";
                throw new Exception(msg);
            }
            else
            {
                string msg = await res.Content.ReadAsStringAsync();
                HelperService.WriteToLog($"Finding nearest airport: {res.ReasonPhrase}");
                throw new Exception($"Finding nearest airport: {res.ReasonPhrase}");
            }
        }

        /// <summary>
        /// Posts a flight log to the api
        /// </summary>
        /// <param name="flightLog">FlightLog data to send</param>
        /// <returns>true of logged successfully</returns>
        public async Task<bool> PostFlightLogAsync(FlightLog flightLog)
        {
            HttpResponseMessage res = await _http.PostAsJsonAsync($"{baseUrl}/log", flightLog);
            if (res.StatusCode == HttpStatusCode.Created)
            {
                return true;
            }
            else
            {
                string msg = await res.Content.ReadAsStringAsync();
                HelperService.WriteToLog(msg);
                return false;
            }
        }

        /// <summary>
        /// Submits pirep data to api
        /// </summary>
        /// <param name="pirep">Pirep data</param>
        /// <returns>true if submission was successful</returns>
        public async Task<bool> PostPirepAsync(Pirep pirep)
        {
            HttpResponseMessage res = await _http.PostAsJsonAsync($"{baseUrl}/pirep/submit", pirep);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                string msg = await res.Content.ReadAsStringAsync();
                HelperService.WriteToLog(msg);
                return false;
            }
        }

        /// <summary>
        /// Updates pirep to cancel progress and reset
        /// </summary>
        /// <returns>true if successful</returns>
        public async Task<bool> CancelTrackingAsync()
        {
            HttpResponseMessage res = await _http.GetAsync($"{baseUrl}/pirep/reset");
            if (res.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                string msg = await res.Content.ReadAsStringAsync();
                HelperService.WriteToLog(msg);
                return false;
            }
        }

        /// <summary>
        /// Posts a status update for pirep
        /// </summary>
        /// <param name="pirepStatus">Status to send</param>
        /// <returns>true if successful</returns>
        public async Task PostPirepStatusAsync(PirepStatus pirepStatus)
        {
            await _http.PostAsJsonAsync($"{baseUrl}/pirep/status", pirepStatus);
        }

        public async Task<ICollection<AddonResource>> GetAddonResources()
        {
            HttpResponseMessage res = await _http.GetAsync($"{baseUrl}/resources");
            if (res.StatusCode == HttpStatusCode.OK)
            {
                var ret = await res.Content.ReadFromJsonAsync<AddonResult>(HelperService.SerializerOptions);
                return ret.AddonResources;
            }
            else if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var msg = "Unauthorised";
                throw new Exception(msg);
            }
            else
            {
                string msg = await res.Content.ReadAsStringAsync();
                HelperService.WriteToLog($"Retrieving addons: {res.ReasonPhrase}");
                throw new Exception($"Retrieving addons: {res.ReasonPhrase}");
            }
        }

        //public async Task<int?> PostGetDistance(object points)
        //{
        //    AddHeaders();
        //    HttpResponseMessage res = await _http.PostAsJsonAsync($"{baseUrl}/tracker/distance", points);
        //    if (res.StatusCode == HttpStatusCode.OK)
        //    {
        //        return await res.Content.ReadFromJsonAsync<int>();
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        /// <summary>
        /// Adds default headers to a request
        /// </summary>
        protected void AddAuthHeaders()
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Properties.Settings.Default.Key);
        }
    }
}
