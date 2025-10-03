using System.Net.Http;
using System.Text.Json;

namespace WeatherOracle.Services
{
    public class PrecomputeService
    {
        private readonly string cachePath = Path.Combine("Cache", "demo_cities.json");
        private readonly HttpClient _http = new HttpClient();

        private readonly Dictionary<string, (double Lat, double Lon)> Cities = new()
        {
            { "Paris", (48.8566, 2.3522) },
            { "Cairo", (30.0444, 31.2357) },
            { "New York", (40.7128, -74.0060) },
            { "Mumbai", (19.0760, 72.8777) },
            { "Sydney", (-33.8688, 151.2093) },
            { "London", (51.5074, -0.1278) },
            { "Rio", (-22.9068, -43.1729) },
        };

        public async Task GenerateCacheAsync()
        {
            var output = new Dictionary<string, object>();

            foreach (var city in Cities)
            {
                Console.WriteLine($"Computing: {city.Key}");

                var months = new Dictionary<string, object>();

                for (int m = 1; m <= 12; m++)
                {
                    var powerData = await FetchPowerDaily(city.Value.Lat, city.Value.Lon);
                    double? heatProb = ComputeProbExceedance(powerData, "T2M", m, 35.0);
                    double? rainProb = ComputeProbExceedance(powerData, "PRECTOTCORR", m, 20.0);

                    months[m.ToString()] = new
                    {
                        heat_above_35C = heatProb.HasValue ? Math.Round(heatProb.Value, 3) : (double?)null,
                        rain_above_20mm = rainProb.HasValue ? Math.Round(rainProb.Value, 3) : (double?)null
                    };
                }

                output[$"{city.Value.Lat},{city.Value.Lon}"] = new Dictionary<string, object>
                {
                    { "name", city.Key }
                }.Concat(months).ToDictionary(x => x.Key, x => x.Value);
            }

            Directory.CreateDirectory("Cache");
            var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(cachePath, json);

            Console.WriteLine($"Wrote: {cachePath}");
        }

        private async Task<JsonDocument> FetchPowerDaily(double lat, double lon)
        {
            // NASA POWER daily data (1981–2010 climatology)
            string url = $"https://power.larc.nasa.gov/api/temporal/climatology/point?parameters=T2M,PRECTOTCORR&community=RE&longitude={lon}&latitude={lat}&format=JSON";

            var response = await _http.GetStringAsync(url);
            return JsonDocument.Parse(response);
        }

        private double? ComputeProbExceedance(JsonDocument powerData, string param, int month, double threshold)
        {
            try
            {
                var root = powerData.RootElement
                    .GetProperty("properties")
                    .GetProperty("parameter")
                    .GetProperty(param);

                // NASA returns average per month (climatology)
                double value = root.GetProperty(month.ToString("D2")).GetDouble();

                // Probability: since climatology gives *mean*, we approximate
                return value > threshold ? 1.0 : 0.0;
            }
            catch
            {
                return null;
            }
        }
    }
}
