using RedditSentimentTrader.Api.Data;
using RedditSentimentTrader.Api.Repositories;

namespace RedditSentimentTrader.Api.Services
{
    public class RedditPostService : IRedditPostService
    {
        private readonly IRedditPostRepository _repo;

        public RedditPostService(IRedditPostRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<RedditPost>> GetAllAsync() =>
            _repo.GetAllAsync();

        public Task<RedditPost?> GetByIdAsync(int id) =>
            _repo.GetByIdAsync(id);

        public Task<RedditPost> CreateAsync(RedditPost post) =>
            _repo.CreateAsync(post);

        public Task<RedditPost?> UpdateAsync(RedditPost post) =>
            _repo.UpdateAsync(post);

        public Task<bool> DeleteAsync(int id) =>
            _repo.DeleteAsync(id);
    }
}
