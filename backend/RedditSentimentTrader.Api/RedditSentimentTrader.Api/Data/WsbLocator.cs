using System.Text.Json;
using HtmlAgilityPack;

namespace RedditSentimentTrader.Api.Services
{
    public class WsbLocator
    {
        private readonly IRedditAuthService _auth;
        private readonly IHttpClientFactory _http;

        public WsbLocator(IRedditAuthService auth, IHttpClientFactory http)
        {
            _auth = auth;
            _http = http;
        }

        public async Task<string> FindDailyThreadAsync()
        {
            var token = await _auth.GetValidAccessTokenAsync();
            var client = _http.CreateClient("RedditAPI");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);

            var res = await client.GetAsync("https://oauth.reddit.com/r/wallstreetbets/hot?limit=50&show=all");
            res.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

            foreach (var child in doc.RootElement.GetProperty("data").GetProperty("children").EnumerateArray())
            {
                var data = child.GetProperty("data");
                var title = data.GetProperty("title").GetString();

                if (title.Contains("Daily Discussion Thread", StringComparison.OrdinalIgnoreCase))
                    return "https://www.reddit.com" + data.GetProperty("permalink").GetString();
            }

            throw new Exception("Daily thread not found.");
        }

        public async Task<string> FindWeekendThreadAsync()
        {
            var token = await _auth.GetValidAccessTokenAsync();
            var client = _http.CreateClient("RedditAPI");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
            var res = await client.GetAsync("https://oauth.reddit.com/r/wallstreetbets/hot?limit=50&show=all");

            res.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

            foreach (var child in doc.RootElement.GetProperty("data").GetProperty("children").EnumerateArray())
            {
                var data = child.GetProperty("data");
                var title = data.GetProperty("title").GetString();

                if (title.Contains("Weekend Discussion Thread", StringComparison.OrdinalIgnoreCase))
                    return "https://www.reddit.com" + data.GetProperty("permalink").GetString();
            }

            throw new Exception("Weekend thread not found.");
        }


        public async Task<string> FindMovesTomorrowThreadAsync()
        {
            var token = await _auth.GetValidAccessTokenAsync();

            var client = _http.CreateClient("RedditAPI");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);

            var res = await client.GetAsync("https://oauth.reddit.com/r/wallstreetbets/new?limit=50&show=all");
            res.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

            foreach (var child in doc.RootElement.GetProperty("data").GetProperty("children").EnumerateArray())
            {
                var data = child.GetProperty("data");
                var title = data.GetProperty("title").GetString()?.ToLower() ?? "";

                if (title.Contains("moves tomorrow") ||
                    title.Contains("what are your moves tomorrow"))
                {
                    return "https://www.reddit.com" + data.GetProperty("permalink").GetString();
                }
            }

            throw new Exception("No 'moves tomorrow' thread found on WSB.");
        }



    }
}
