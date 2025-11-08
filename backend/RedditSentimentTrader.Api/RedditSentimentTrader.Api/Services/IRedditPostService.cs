using System.Collections.Generic;
using System.Threading.Tasks;
using RedditSentimentTrader.Api.Data;

namespace RedditSentimentTrader.Api.Services
{
    public interface IRedditPostService
    {
        Task<List<RedditPost>> GetAllAsync();
        Task<RedditPost> CreateAsync(RedditPost post);
    }
}
