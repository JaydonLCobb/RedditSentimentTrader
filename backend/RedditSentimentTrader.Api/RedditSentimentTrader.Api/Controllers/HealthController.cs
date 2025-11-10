using Microsoft.AspNetCore.Mvc;

namespace RedditSentimentTrader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "OK",
                message = "running",
                time = DateTime.UtcNow
            });
        }
    }
}

