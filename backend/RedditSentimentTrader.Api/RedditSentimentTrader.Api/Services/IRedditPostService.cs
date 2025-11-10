using RedditSentimentTrader.Api.Data;

namespace RedditSentimentTrader.Api.Services
{
    public interface IRedditPostService
    {
        Task<IEnumerable<RedditPost>> GetAllAsync();
        Task<RedditPost?> GetByIdAsync(int id);
        Task<RedditPost> CreateAsync(RedditPost post);
        Task<RedditPost?> UpdateAsync(RedditPost post);
        Task<bool> DeleteAsync(int id);
    }
}
