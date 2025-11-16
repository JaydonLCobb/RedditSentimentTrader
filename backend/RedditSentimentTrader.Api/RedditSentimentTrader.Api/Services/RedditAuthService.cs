using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RedditSentimentTrader.Api.Data;
using RedditSentimentTrader.Api.Options;

namespace RedditSentimentTrader.Api.Services
{
    public class RedditAuthService : IRedditAuthService
    {
        private readonly RedditOptions _opts;
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpFactory;

        public RedditAuthService(IOptions<RedditOptions> opts, AppDbContext db, IHttpClientFactory httpFactory)
        {
            _opts = opts.Value;
            _db = db;
            _httpFactory = httpFactory;
        }

        public async Task<string> GetValidAccessTokenAsync()
        {
            var token = await _db.RedditTokens.AsNoTracking().FirstOrDefaultAsync();
            if (token == null || string.IsNullOrWhiteSpace(token.RefreshToken))
                throw new InvalidOperationException("Reddit OAuth not initialized. Visit /auth/reddit/login to authorize.");

            // If < 90 seconds to expiry, refresh.
            if (DateTime.UtcNow >= token.ExpiresAtUtc.AddSeconds(-90))
            {
                token = await RefreshAsync(token.RefreshToken);
                await UpsertAsync(token);
            }

            return token.AccessToken;
        }

        public async Task BootstrapWithAuthCodeAsync(string code, string state)
        {
            var http = _httpFactory.CreateClient();
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token");
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            req.Headers.UserAgent.ParseAdd(_opts.UserAgent);

            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _opts.RedirectUri
            });

            var res = await http.SendAsync(req);
            res.EnsureSuccessStatusCode();

            using var s = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(s);

            var access = doc.RootElement.GetProperty("access_token").GetString()!;
            var refresh = doc.RootElement.GetProperty("refresh_token").GetString()!;
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

            await UpsertAsync(new RedditTokenStore
            {
                AccessToken = access,
                RefreshToken = refresh,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn)
            });
        }

        private async Task<RedditTokenStore> RefreshAsync(string refreshToken)
        {
            var http = _httpFactory.CreateClient();
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token");
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            req.Headers.UserAgent.ParseAdd(_opts.UserAgent);

            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });

            var res = await http.SendAsync(req);
            res.EnsureSuccessStatusCode();

            using var s = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(s);

            var access = doc.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

            return new RedditTokenStore
            {
                AccessToken = access,
                RefreshToken = refreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn)
            };
        }

        private async Task UpsertAsync(RedditTokenStore incoming)
        {
            var existing = await _db.RedditTokens.FirstOrDefaultAsync();

            if (existing == null)
            {
                // First time: insert new row
                _db.RedditTokens.Add(incoming);
            }
            else
            {
                // existing 
                existing.AccessToken = incoming.AccessToken;
                existing.RefreshToken = incoming.RefreshToken;
                existing.ExpiresAtUtc = incoming.ExpiresAtUtc;
            }

            await _db.SaveChangesAsync();
        }
    }
}
