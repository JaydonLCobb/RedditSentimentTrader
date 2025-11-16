using System.Threading.Tasks;

namespace RedditSentimentTrader.Api.Services
{
    public interface IRedditAuthService
    {
        Task<string> GetValidAccessTokenAsync();
        Task BootstrapWithAuthCodeAsync(string code, string state);
    }
}
