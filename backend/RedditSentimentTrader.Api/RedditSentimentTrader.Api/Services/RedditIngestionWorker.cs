using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using RedditSentimentTrader.Api.Data;
using RedditSentimentTrader.Api.Services;
using RedditSentimentTrader.Api.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace RedditSentimentTrader.Api.Services
{
    public class RedditIngestionWorker : BackgroundService
    {
        private readonly ILogger<RedditIngestionWorker> _logger;
        private readonly IServiceProvider _services;
        private readonly HttpClient _http = new HttpClient();

        public RedditIngestionWorker(ILogger<RedditIngestionWorker> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await FetchAndStorePosts();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inside Reddit ingestion");
                }

                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        private async Task FetchAndStorePosts()
        {
            _logger.LogInformation("getting Reddit posts");

            string url =
                "https://api.pullpush.io/reddit/search/comment/?subreddit=wallstreetbets&size=5";

            PushshiftResponse? result = null;

            try
            {
                result = await _http.GetFromJsonAsync<PushshiftResponse>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "deserialize error");
                return;
            }

            if (result?.data == null || result.data.Count == 0)
            {
                _logger.LogWarning("returned no data");
                return;
            }

            using var scope = _services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRedditPostService>();

            int savedCount = 0;

            foreach (var c in result.data)
            {
                if (string.IsNullOrWhiteSpace(c.body))
                    continue;

                var post = new RedditPost
                {
                    Ticker = "SPY", 
                    Author = c.author ?? "unknown",
                    Content = c.body,
                    SentimentScore = 0.0,  
                    CreatedUtc = DateTimeOffset.FromUnixTimeSeconds((long)Math.Floor(c.created_utc)).UtcDateTime
                };

                await service.CreateAsync(post);
                savedCount++;
            }

            _logger.LogInformation($"svd {savedCount} posts to db");
        }
    }

    public class PushshiftResponse
    {
        public List<PushshiftComment> data { get; set; } = new();
    }

    public class PushshiftComment
    {
        public string body { get; set; }
        public string author { get; set; }
        public double created_utc { get; set; }
    }
}
