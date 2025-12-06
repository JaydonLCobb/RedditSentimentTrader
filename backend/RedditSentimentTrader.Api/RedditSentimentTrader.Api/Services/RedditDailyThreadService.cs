using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RedditSentimentTrader.Api.Data;

namespace RedditSentimentTrader.Api.Services
{
    public interface IRedditDailyThreadService
    {
        Task<int> IngestThreadAsync(string threadUrl);
    }

    public class RedditDailyThreadService : IRedditDailyThreadService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IRedditAuthService _auth;
        private readonly ISentimentService _sentiment;
        private readonly ITickerExtractionService _tickerExtraction;

        public RedditDailyThreadService(
            AppDbContext db,
            IHttpClientFactory clientFactory,
            IRedditAuthService auth,
            ISentimentService sentiment,
            ITickerExtractionService tickerExtraction)
        {
            _db = db;
            _clientFactory = clientFactory;
            _auth = auth;
            _sentiment = sentiment;
            _tickerExtraction = tickerExtraction;
        }

        public async Task<int> IngestThreadAsync(string threadUrl)
        {
            if (string.IsNullOrWhiteSpace(threadUrl))
                throw new ArgumentException("threadUrl is required", nameof(threadUrl));

            // e.g. https://www.reddit.com/r/wallstreetbets/comments/1p1jm4l/what_are_your_moves_tomorrow_november_20_2025/
            var uri = new Uri(threadUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var commentsIndex = Array.IndexOf(segments, "comments");
            if (commentsIndex < 0 || commentsIndex + 1 >= segments.Length)
                throw new InvalidOperationException($"Could not parse thread id from URL '{threadUrl}'.");

            var threadId = segments[commentsIndex + 1];

            var apiUrl =
                $"https://oauth.reddit.com/comments/{threadId}.json?depth=10&limit=250&raw_json=1";

            var token = await _auth.GetValidAccessTokenAsync();

            var client = _clientFactory.CreateClient("RedditAPI");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var res = await client.GetAsync(apiUrl);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // [0] = post, [1] = comments
            var commentsArr = doc.RootElement[1]
                                 .GetProperty("data")
                                 .GetProperty("children");

            var flat = new List<RedditComment>();
            foreach (var child in commentsArr.EnumerateArray())
                Flatten(child, flat, threadUrl);

            var total = flat.Count;
            var skippedExists = 0;
            var skippedShort = 0;
            var skippedNoTicker = 0;
            var added = 0;


            foreach (var c in flat)
            {
                bool exists = await _db.RedditComments
                    .AnyAsync(x => x.RedditId == c.RedditId);

                if (exists)
                {
                    skippedExists++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(c.Body) || c.Body.Length < 5)
                {
                    skippedShort++;
                    continue;
                }

                var extraction = await _tickerExtraction.ExtractAsync(c.Body);

                if (!extraction.IsMarketRelated ||
                    string.IsNullOrWhiteSpace(extraction.PrimaryTicker))
                {
                    skippedNoTicker++;
                    continue;
                }

                c.IsMarketRelated = true;
                c.PrimaryTicker = extraction.PrimaryTicker;
                // c.RawTickers = string.Join(",", extraction.Tickers);

                var bodyForModel =
                    c.Body.Length > 1000 ? c.Body[..1000] : c.Body;

                var sentiment = await _sentiment.ScoreAsync(bodyForModel);

                c.SentimentLabel = sentiment.Label;
                c.SentimentScore = sentiment.Score;
                c.Confidence = sentiment.Confidence;

                _db.RedditComments.Add(c);
                added++;
            }
            await _db.SaveChangesAsync();

            //_logger.LogInformation("IngestThreadAsync: total={Total}, added={Added}, exists={Exists}, short={Short}, noTicker={NoTicker}",total, added, skippedExists, skippedShort, skippedNoTicker);
            Console.WriteLine(added);
            return flat.Count;
        }

        private void Flatten(JsonElement element, List<RedditComment> output, string threadUrl)
        {
            if (element.GetProperty("kind").GetString() != "t1")
                return;

            var data = element.GetProperty("data");

            long createdUnix = 0;

            if (data.TryGetProperty("created_utc", out var createdProp))
            {
                switch (createdProp.ValueKind)
                {
                    case JsonValueKind.Number:
                        if (!createdProp.TryGetInt64(out createdUnix))
                        {
                            var dbl = createdProp.GetDouble();
                            createdUnix = (long)dbl;
                        }
                        break;

                    case JsonValueKind.String:
                        var s = createdProp.GetString();
                        if (long.TryParse(s, out var parsedInt))
                            createdUnix = parsedInt;
                        else if (double.TryParse(s, out var parsedDouble))
                            createdUnix = (long)parsedDouble;
                        break;
                }
            }

            if (createdUnix == 0)
                createdUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            output.Add(new RedditComment
            {
                RedditId = data.GetProperty("id").GetString() ?? "",
                Author = data.GetProperty("author").GetString() ?? "",
                Body = data.GetProperty("body").GetString() ?? "",
                CreatedUtc = DateTimeOffset.FromUnixTimeSeconds(createdUnix).UtcDateTime,
                ThreadUrl = threadUrl
            });

            if (data.TryGetProperty("replies", out var replies) &&
                replies.ValueKind != JsonValueKind.String)
            {
                var children = replies.GetProperty("data").GetProperty("children");

                foreach (var r in children.EnumerateArray())
                    Flatten(r, output, threadUrl);
            }
        }
    }
}
