using Microsoft.AspNetCore.Mvc;
using RedditSentimentTrader.Api.Services;

[ApiController]
[Route("reddit/wsb")]
public class WsbController : ControllerBase
{
    private readonly IRedditDailyThreadService _ingest;
    private readonly WsbLocator _locator;

    public WsbController(
        IRedditDailyThreadService ingest,
        WsbLocator locator)
    {
        _ingest = ingest;
        _locator = locator;
    }

    [HttpGet("today")]
    public async Task<IActionResult> Today()
    {
        string threadUrl = await _locator.FindDailyThreadAsync();

        int imported = await _ingest.IngestThreadAsync(threadUrl);

        return Ok(new
        {
            thread = threadUrl,
            commentsImported = imported
        });
    }

    [HttpGet("weekend")]
    public async Task<IActionResult> Weekend()
    {
        string threadUrl = await _locator.FindWeekendThreadAsync();

        int imported = await _ingest.IngestThreadAsync(threadUrl);

        return Ok(new
        {
            thread = threadUrl,
            commentsImported = imported
        });
    }

    [HttpGet("moves-tomorrow")]
    public async Task<IActionResult> MovesTomorrow()
    {
        string threadUrl = await _locator.FindMovesTomorrowThreadAsync();

        int imported = await _ingest.IngestThreadAsync(threadUrl);

        return Ok(new
        {
            thread = threadUrl,
            commentsImported = imported
        });
    }
}
