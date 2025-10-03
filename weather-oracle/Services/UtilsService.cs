using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;


namespace weather_oracle.Services
{
    public class UtilsService
    {
        private readonly HttpClient _http;

        public UtilsService(HttpClient httpClient)
        {
            _http = httpClient;
        }

        private const string POWER_BASE = "https://power.larc.nasa.gov/api/temporal/daily/point";

        // Fetch POWER data
        public async Task<JObject> FetchPowerDaily(double lat, double lon,
            string startDate = "19810101", string endDate = "20101231",
            string parameters = "T2M,PRECTOTCORR")
        {
            var url = $"{POWER_BASE}?parameters={parameters}&community=AG&latitude={lat}&longitude={lon}&start={startDate}&end={endDate}&format=JSON";
            var response = await _http.GetStringAsync(url);
            return JObject.Parse(response);
        }

        // Compute probability of exceedance
        public double? ComputeProbExceedance(JObject powerJson, string param, int month, double threshold)
        {
            var series = powerJson["properties"]?["parameter"]?[param] as JObject;
            if (series == null) return null;

            var data = series.Properties()
                .Select(p => new { Date = DateTime.ParseExact(p.Name, "yyyyMMdd", null), Value = (double)p.Value })
                .Where(d => d.Date.Month == month)
                .ToList();

            if (data.Count == 0) return null;

            int exceedCount = data.Count(d => d.Value > threshold);
            return Math.Round((double)exceedCount / data.Count, 3);
        }
    }
}
