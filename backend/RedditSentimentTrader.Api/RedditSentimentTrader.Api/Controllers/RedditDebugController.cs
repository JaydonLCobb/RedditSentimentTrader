using Microsoft.AspNetCore.Mvc;
using RedditSentimentTrader.Api.Services;

namespace RedditSentimentTrader.Api.Controllers
{
    [ApiController]
    [Route("debug/reddit")]
    public class RedditDebugController : ControllerBase
    {
        private readonly IRedditAuthService _auth;

        public RedditDebugController(IRedditAuthService auth)
        {
            _auth = auth;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var token = await _auth.GetValidAccessTokenAsync();

            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("SentimentTrader/1.0 by u/whosmirage");
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var res = await http.GetAsync("https://oauth.reddit.com/api/v1/me");

            var content = await res.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
    }
}
