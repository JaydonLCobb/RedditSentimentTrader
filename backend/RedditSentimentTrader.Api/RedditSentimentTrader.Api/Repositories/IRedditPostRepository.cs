using System.Collections.Generic;
using System.Threading.Tasks;
using RedditSentimentTrader.Api.Data;

namespace RedditSentimentTrader.Api.Repositories
{
    public interface IRedditPostRepository
    {
        Task<List<RedditPost>> GetAllAsync();
        Task<RedditPost> AddAsync(RedditPost post);
    }
}
