using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RedditSentimentTrader.Api.Options;
using RedditSentimentTrader.Api.Services;
using System.Web;

namespace RedditSentimentTrader.Api.Controllers
{
    [ApiController]
    [Route("auth/reddit")]
    public class AuthController : ControllerBase
    {
        private readonly RedditOptions _opts;
        private readonly IRedditAuthService _authService;

        public AuthController(IOptions<RedditOptions> opts, IRedditAuthService authService)
        {
            _opts = opts.Value;
            _authService = authService;
        }

        // /auth/reddit/login
        [HttpGet("login")]
        public IActionResult Login()
        {
                        var state = Guid.NewGuid().ToString("N");

            var url = "https://www.reddit.com/api/v1/authorize" +
                      $"?client_id={HttpUtility.UrlEncode(_opts.ClientId)}" +
                      $"&response_type=code" +
                      $"&state={state}" +
                      $"&redirect_uri={HttpUtility.UrlEncode(_opts.RedirectUri)}" +
                      $"&duration=permanent" +
                      $"&scope={HttpUtility.UrlEncode(_opts.Scope)}";

            return Redirect(url);
        }

        // auth/reddit/callback?code=...&state=...
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            await _authService.BootstrapWithAuthCodeAsync(code, state);
            return Ok(new { status = "authorized" });
        }

        [HttpGet("debug-config")]
        public object DebugConfig([FromServices] IConfiguration config)
        {
            return new
            {
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                conn = config.GetConnectionString("DefaultConnection"),
                clientId = config["Reddit:ClientId"],
                vault = config["KeyVaultUri"]
            };
        }
    }
}

