using Microsoft.AspNetCore.Mvc;
using RedditSentimentTrader.Api.Services;

namespace RedditSentimentTrader.Api.Controllers
{
    [ApiController]
    [Route("reddit")]
    public class RedditController : ControllerBase
    {
        private readonly IRedditApiService _reddit;

        public RedditController(IRedditApiService reddit)
        {
            _reddit = reddit;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var result = await _reddit.GetMeAsync();
            return Ok(result);
        }
        [HttpGet("r/{subreddit}")]
        public async Task<IActionResult> Subreddit(string subreddit, [FromQuery] string sort = "hot")
        {
            var result = await _reddit.GetSubredditPostsAsync(subreddit, sort);
            return Ok(result);
        }
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string ticker)
        {
            var query = $"title:{ticker}";
            var result = await _reddit.SearchAsync(query);
            return Ok(result);
        }
    }
}
