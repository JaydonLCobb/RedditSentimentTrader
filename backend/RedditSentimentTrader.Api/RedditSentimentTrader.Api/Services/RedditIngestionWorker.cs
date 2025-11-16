using RedditSentimentTrader.Api.Services;

public class RedditIngestionWorker : BackgroundService
{
    private readonly ILogger<RedditIngestionWorker> _logger;
    private readonly IServiceProvider _services;
    private readonly IHttpClientFactory _httpFactory;

    public RedditIngestionWorker(
        ILogger<RedditIngestionWorker> logger,
        IServiceProvider services,
        IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _services = services;
        _httpFactory = httpFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reddit worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAndStorePosts(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ingestion loop");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task FetchAndStorePosts(CancellationToken token)
    {
        _logger.LogInformation("Fetching Reddit posts...");

        using var scope = _services.CreateScope();

      
        var auth = scope.ServiceProvider.GetRequiredService<IRedditAuthService>();
        var postService = scope.ServiceProvider.GetRequiredService<IRedditPostService>();

        var accessToken = await auth.GetValidAccessTokenAsync();

        var http = _httpFactory.CreateClient("RedditAPI");
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", accessToken);

        var url = "https://oauth.reddit.com/r/stocks/comments?limit=5";
        var res = await http.GetAsync(url, token);
        res.EnsureSuccessStatusCode();

        using var stream = await res.Content.ReadAsStreamAsync(token);
        using var json = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: token);

        // TODO: map → save to DB using postService
        _logger.LogInformation("Fetched posts from Reddit API.");
    }
}
