using System.Net.Http.Headers;
using System.Text.Json;
using RedditSentimentTrader.Api.Data;

namespace RedditSentimentTrader.Api.Services
{
    public interface IRedditApiService
    {
        Task<JsonElement> GetMeAsync();
        Task<JsonElement> GetSubredditPostsAsync(string subreddit, string sort);
        Task<JsonElement> SearchAsync(string query);
    }

    public class RedditApiService : IRedditApiService
    {
        private readonly IRedditAuthService _auth;
        private readonly IHttpClientFactory _httpFactory;

        public RedditApiService(IRedditAuthService auth, IHttpClientFactory httpFactory)
        {
            _auth = auth;
            _httpFactory = httpFactory;
        }

        private async Task<HttpClient> CreateClientAsync()
        {
            var access = await _auth.GetValidAccessTokenAsync();
            var client = _httpFactory.CreateClient();

            client.BaseAddress = new Uri("https://oauth.reddit.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SentimentTrader/1.0");

            return client;
        }

        public async Task<JsonElement> GetMeAsync()
        {
            var http = await CreateClientAsync();
            var res = await http.GetAsync("api/v1/me");
            res.EnsureSuccessStatusCode();

            var stream = await res.Content.ReadAsStreamAsync();
            return JsonDocument.Parse(stream).RootElement.Clone();
        }

        public async Task<JsonElement> GetSubredditPostsAsync(string subreddit, string sort = "hot")
        {
            var http = await CreateClientAsync();
            var res = await http.GetAsync($"/r/{subreddit}/{sort}?limit=25");
            res.EnsureSuccessStatusCode();

            var stream = await res.Content.ReadAsStreamAsync();
            return JsonDocument.Parse(stream).RootElement.Clone();
        }

        public async Task<JsonElement> SearchAsync(string query)
        {
            var http = await CreateClientAsync();
            var res = await http.GetAsync($"/search?q={Uri.EscapeDataString(query)}&limit=25");
            res.EnsureSuccessStatusCode();

            var stream = await res.Content.ReadAsStreamAsync();
            return JsonDocument.Parse(stream).RootElement.Clone();
        }
    }
}
