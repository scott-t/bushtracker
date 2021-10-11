using BushDiversTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace BushDiversTracker.Services
{
    class APIService
    {
        protected string baseUrl = "https://forum.bushdivers.com/api";
        // protected string baseUrl = "http://localhost:8000/api";
        HttpClient _http = new HttpClient();

        public async Task<ICollection<DispatchCargo>> GetDispatchCargoAsync()
        {
            AddHeaders();
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

        public async Task<Dispatch> GetDispatchInfoAsync()
        {
            AddHeaders();
            HttpResponseMessage res = await _http.GetAsync($"{baseUrl}/dispatch");
            if (res.StatusCode == HttpStatusCode.OK)
            {
                return await res.Content.ReadFromJsonAsync<Dispatch>();
            }
            else if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                var msg = "Unauthorised";
                throw new Exception(msg);
            }
            else
            {
                string msg = await res.Content.ReadAsStringAsync();
                HelperService.WriteToLog($"Fetching dispatch info: {res.ReasonPhrase}");
                throw new Exception($"Fetching dispatch info: {res.ReasonPhrase}");
            }
        }

        public async Task<NewLocationResponse> PostNewLocationAsync(NewLocationRequest newLocation)
        {
            AddHeaders();
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

        public async Task<bool> PostFlightLogAsync(FlightLog flightLog)
        {
            AddHeaders();
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

        public async Task<bool> PostPirepAsync(Pirep pirep)
        {
            AddHeaders();
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

        public async Task<bool> CancelTrackingAsync()
        {
            AddHeaders();
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

        public async Task<bool> PostPirepStatusAsync(PirepStatus pirepStatus)
        {
            AddHeaders();
            HttpResponseMessage res = await _http.PostAsJsonAsync($"{baseUrl}/pirep/status", pirepStatus);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                return false;
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

        protected void AddHeaders()
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Properties.Settings.Default.Key);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
