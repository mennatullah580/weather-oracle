using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherOracle.Services
{
    public class PowerService
    {
        private readonly HttpClient _httpClient;
        private const string POWER_BASE = "https://power.larc.nasa.gov/api/temporal/daily/point";

        public PowerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<PowerResponse> FetchPowerDailyAsync(
            double lat,
            double lon,
            string startDate = "19810101",
            string endDate = "20101231",
            string parameters = "T2M,PRECTOTCORR")
        {
            var queryParams = new Dictionary<string, string>
            {
                { "parameters", parameters },
                { "community", "AG" },
                { "latitude", lat.ToString("F4") },
                { "longitude", lon.ToString("F4") },
                { "start", startDate },
                { "end", endDate },
                { "format", "JSON" }
            };

            var queryString = string.Join("&", queryParams.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var url = $"{POWER_BASE}?{queryString}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var powerResponse = JsonSerializer.Deserialize<PowerResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return powerResponse;
        }

        public double? ComputeProbExceedance(
            PowerResponse powerData,
            string parameter,
            int month,
            double threshold)
        {
            if (powerData?.Properties?.Parameter == null)
                return null;

            if (!powerData.Properties.Parameter.TryGetValue(parameter, out var series))
                throw new ArgumentException($"Parameter {parameter} not found in POWER response.");

            var monthData = series
                .Where(kvp => {
                    if (DateTime.TryParseExact(kvp.Key, "yyyyMMdd", null,
                        System.Globalization.DateTimeStyles.None, out var date))
                    {
                        return date.Month == month;
                    }
                    return false;
                })
                .Select(kvp => {
                    if (double.TryParse(kvp.Value, out var val))
                        return (double?)val;
                    return null;
                })
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            if (monthData.Count == 0)
                return null;

            int exceedCount = monthData.Count(v => v > threshold);
            return (double)exceedCount / monthData.Count;
        }
    }

    // Data models for NASA POWER API response
    public class PowerResponse
    {
        public PowerProperties Properties { get; set; }
    }

    public class PowerProperties
    {
        public Dictionary<string, Dictionary<string, string>> Parameter { get; set; }
    }
}
