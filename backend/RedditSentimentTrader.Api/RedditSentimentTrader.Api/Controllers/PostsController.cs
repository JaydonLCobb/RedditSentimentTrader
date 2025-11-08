using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedditSentimentTrader.Api.Data;
using RedditSentimentTrader.Api.Services;

namespace RedditSentimentTrader.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {

        private readonly IRedditPostService _service;

        public PostsController(IRedditPostService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var posts = await _service.GetAllAsync();
            return Ok(posts);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RedditPost post)
        {
            var created = await _service.CreateAsync(post);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }



    }
}
