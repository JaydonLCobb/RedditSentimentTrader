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

        
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var post = await _service.GetByIdAsync(id);
            if (post == null)
                return NotFound();

            return Ok(post);
        }

        
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RedditPost post)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(post);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] RedditPost post)
        {
            if (id != post.Id)
                return BadRequest("url id != body.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(post);

            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
