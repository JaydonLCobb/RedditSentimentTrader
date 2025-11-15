using Microsoft.AspNetCore.Mvc;
using RedditSentimentTrader.Api.Services;

namespace RedditSentimentTrader.Api.Controllers
{
    [ApiController]
    [Route("reddit/wsb")]
    public class WsbController : ControllerBase
    {
        private readonly IWsbDailyService _daily;
        private readonly IRedditApiService _reddit;

        public WsbController(IWsbDailyService daily, IRedditApiService reddit)
        {
            _daily = daily;
            _reddit = reddit;
        }

        // /reddit/wsb/today
        [HttpGet("today")]
        public async Task<IActionResult> Today()
        {
            var post = await _daily.FindTodayDiscussionThreadAsync();
            if (post is null)
                return NotFound("No daily discussion thread found today.");

            return Ok(post);
        }

        //  /reddit/wsb/weekend
        [HttpGet("weekend")]
        public async Task<IActionResult> Weekend()
        {
            var post = await _daily.FindWeekendThreadAsync();
            if (post is null)
                return NotFound("No weekend discussion thread found.");

            return Ok(post);
        }
    }
}
