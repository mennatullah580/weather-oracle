using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherOracle.Services;

namespace WeatherOracle.Controllers
{
    [ApiController]
    [Route("api")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly PowerService _powerService;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            PowerService powerService)
        {
            _logger = logger;
            _powerService = powerService;
        }

        [HttpGet("likelihood")]
        public async Task<IActionResult> GetLikelihood(
            [FromQuery] double? lat,
            [FromQuery] double? lon,
            [FromQuery] int month = 7,
            [FromQuery] string heatParam = "T2M",
            [FromQuery] double heatThresh = 35.0,
            [FromQuery] string rainParam = "PRECTOTCORR",
            [FromQuery] double rainThresh = 20.0)
        {
            try
            {
                if (!lat.HasValue || !lon.HasValue)
                {
                    return BadRequest(new { error = "lat and lon are required" });
                }

                _logger.LogInformation("Received request: lat={Lat}, lon={Lon}, month={Month}",
                    lat.Value, lon.Value, month);

                // Fetch from NASA POWER API
                _logger.LogInformation("Fetching data from NASA POWER API (this may take 20-30 seconds)");
                var powerData = await _powerService.FetchPowerDailyAsync(lat.Value, lon.Value);

                var heatProb = _powerService.ComputeProbExceedance(
                    powerData, heatParam, month, heatThresh);
                var rainProb = _powerService.ComputeProbExceedance(
                    powerData, rainParam, month, rainThresh);

                var result = new
                {
                    location = $"{lat.Value},{lon.Value}",
                    month = month,
                    probabilities = new
                    {
                        heat_above_35C = heatProb.HasValue ? Math.Round(heatProb.Value, 3) : (double?)null,
                        rain_above_20mm = rainProb.HasValue ? Math.Round(rainProb.Value, 3) : (double?)null
                    }
                };

                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "NASA POWER API request failed");
                return StatusCode(502, new
                {
                    error = "Bad Gateway",
                    details = "NASA POWER API call failed",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing request");
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    details = ex.Message
                });
            }
        }
    }
}