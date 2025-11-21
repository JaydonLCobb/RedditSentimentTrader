using RedditSentimentTrader.Api.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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

        public RedditDailyThreadService(AppDbContext db, IHttpClientFactory clientFactory, IRedditAuthService auth)
        {
            _db = db;
            _clientFactory = clientFactory;
            _auth = auth;
        }

        public async Task<int> IngestThreadAsync(string threadUrl)
        {
            if (string.IsNullOrWhiteSpace(threadUrl))
                throw new ArgumentException("threadUrl is required", nameof(threadUrl));

            // https://www.reddit.com/r/wallstreetbets/comments/1p1jm4l/what_are_your_moves_tomorrow_november_20_2025/
            var uri = new Uri(threadUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var commentsIndex = Array.IndexOf(segments, "comments");
            if (commentsIndex < 0 || commentsIndex + 1 >= segments.Length)
                throw new InvalidOperationException($"Could not parse thread id from URL '{threadUrl}'.");

            var threadId = segments[commentsIndex + 1];

            var apiUrl = $"https://oauth.reddit.com/comments/{threadId}.json?depth=10&limit=500&raw_json=1";

            var token = await _auth.GetValidAccessTokenAsync();

            var client = _clientFactory.CreateClient("RedditAPI");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var res = await client.GetAsync(apiUrl);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // [0] = post, [1] = comments
            var commentsArr = doc.RootElement[1].GetProperty("data")
                                              .GetProperty("children");

            var flat = new List<RedditComment>();
            foreach (var child in commentsArr.EnumerateArray())
                Flatten(child, flat, threadUrl);

            foreach (var c in flat)
            {
                bool exists = await _db.RedditComments.AnyAsync(x => x.RedditId == c.RedditId);
                if (!exists)
                    _db.RedditComments.Add(c);
            }

            await _db.SaveChangesAsync();
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
                        createdUnix = createdProp.TryGetInt64(out var i64)
                            ? i64
                            : (long)createdProp.GetDouble();
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
