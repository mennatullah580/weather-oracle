using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using weather_oracle.Services;

namespace weather_oracle.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LikelihoodController : ControllerBase
    {
        private readonly PowerService _power;

        public LikelihoodController(PowerService power)
        {
            _power = power;
        }

        // Example:
        // GET /Likelihood?lat=48.8&lon=2.3&month=7&param=T2M&threshold=30
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] int month,
            [FromQuery] string param = "T2M",
            [FromQuery] double threshold = 30.0,
            [FromQuery] string startDate = "19810101",
            [FromQuery] string endDate = "20101231")
        {
            try
            {
                var powerJson = await _power.FetchPowerDailyAsync(lat, lon, startDate, endDate, parameters: param);
                var prob = _power.ComputeProbExceedance(powerJson, param, month, threshold);

                var probabilities = new
                {
                    exceedance = prob.HasValue ? Math.Round(prob.Value, 4) : (double?)null
                };

                var response = new
                {
                    location = "Unknown",
                    month = month.ToString("D2"),
                    param,
                    threshold,
                    probabilities,
                    query = new { lat, lon, startDate, endDate }
                };

                return Ok(response);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Failed to reach POWER API", details = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message);
            }
        }
    }
}
