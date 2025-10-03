using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace weather_oracle.Services
{
    public class PowerService
    {
        private const string POWER_BASE = "https://power.larc.nasa.gov/api/temporal/daily/point";
        private readonly HttpClient _http;

        public PowerService(HttpClient http)
        {
            _http = http;
        }

        // Fetch JSON from NASA POWER (returns parsed JObject)
        public async Task<JObject> FetchPowerDailyAsync(double lat, double lon,
            string startDate = "19810101", string endDate = "20101231",
            string parameters = "T2M,PRECTOTCORR")
        {
            // build query string and call API
            var query = $"?parameters={Uri.EscapeDataString(parameters)}&community=AG" +
                        $"&latitude={lat}&longitude={lon}&start={startDate}&end={endDate}&format=JSON";

            var resp = await _http.GetAsync(POWER_BASE + query);
            resp.EnsureSuccessStatusCode();
            var text = await resp.Content.ReadAsStringAsync();
            return JObject.Parse(text);
        }

        // Compute exceedance probability for a parameter (e.g. "T2M") for given month and threshold
        // month is 1..12, threshold is the numeric threshold (e.g. 30 for 30°C)
        // returns null if there are no data days for that month
        public double? ComputeProbExceedance(JObject powerJson, string param, int month, double threshold)
        {
            if (powerJson == null) throw new ArgumentNullException(nameof(powerJson));
            if (string.IsNullOrWhiteSpace(param)) throw new ArgumentNullException(nameof(param));
            if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));

            // navigate to properties.parameter.<param>
            var token = powerJson.SelectToken($"properties.parameter.{param}");
            if (token == null || token.Type != JTokenType.Object) return null;

            var obj = (JObject)token;
            var dateValuePairs = new List<(DateTime date, double value)>();

            foreach (var prop in obj.Properties())
            {
                // keys are like "19810101" -> parse as yyyyMMdd
                if (!DateTime.TryParseExact(prop.Name, "yyyyMMdd", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out var dt)) continue;

                // Try parse numeric value (skip non-numeric or missing)
                if (double.TryParse(prop.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                {
                    // skip common missing-data sentinel values if present (e.g. -9999)
                    if (double.IsNaN(val) || val < -9000) continue;
                    dateValuePairs.Add((dt, val));
                }
            }

            var monthPairs = dateValuePairs.Where(x => x.date.Month == month).ToList();
            if (!monthPairs.Any()) return null;

            var exceedCount = monthPairs.Count(x => x.value > threshold);
            return (double)exceedCount / monthPairs.Count;
        }
    }
}
