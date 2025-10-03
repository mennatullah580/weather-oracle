using Microsoft.AspNetCore.Mvc;
using weather_oracle.Services;
using System.Threading.Tasks;

namespace weather_oracle.Controllers
{
    [ApiController]
    [Route("api/[controller]")]   // → /api/likelihood
    public class likelihoodController:ControllerBase
    {
        private readonly UtilsService _utils;

        public likelihoodController(UtilsService utils)
        {
            _utils = utils;
        }

    
    [HttpGet]
        public async Task<IActionResult> GetLikelihood(
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] int month,
            [FromQuery] string heat_param = "T2M",
            [FromQuery] double heat_thresh = 35.0,
            [FromQuery] string rain_param = "PRECTOTCORR",
            [FromQuery] double rain_thresh = 20.0)
        {
            // validate required inputs
            if (lat == 0 || lon == 0)
                return BadRequest(new { error = "lat and lon are required" });

            // Fetch POWER data from NASA
            var powerData = await _utils.FetchPowerDaily(lat, lon);

            // Compute probabilities
            var heatProb = _utils.ComputeProbExceedance(powerData, heat_param, month, heat_thresh);
            var rainProb = _utils.ComputeProbExceedance(powerData, rain_param, month, rain_thresh);

            // Response
            var result = new
            {
                location = $"{lat},{lon}",
                month = month,
                probabilities = new
                {
                    heat_above_35C = heatProb,
                    rain_above_20mm = rainProb
                }
            };

            return Ok(result);
        }
    }
}
