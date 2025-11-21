using Microsoft.AspNetCore.Mvc;
using RedditSentimentTrader.Api.Services;

[ApiController]
[Route("wsb")]
public class DailyThreadController : ControllerBase
{
    private readonly IRedditDailyThreadService _svc;

    public DailyThreadController(IRedditDailyThreadService svc)
    {
        _svc = svc;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] string threadUrl)
    {
        var count = await _svc.IngestThreadAsync(threadUrl);
        return Ok(new { imported = count });
    }
}
