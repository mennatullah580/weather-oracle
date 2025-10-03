using Microsoft.AspNetCore.Mvc;

namespace weather_oracle.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LikelihoodController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromQuery] double lat, [FromQuery] double lon, [FromQuery] string month)
        {
            // Placeholder logic for now
            var response = new
            {
                location = "Unknown",
                month = month,
                probabilities = new
                {
                    extreme_heat = 0.12,
                    heavy_rain = 0.05
                },
                query = new { lat, lon }
            };

            return Ok(response);
        }
    }
}
