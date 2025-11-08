using System.Collections.Generic;
using System.Threading.Tasks;
using RedditSentimentTrader.Api.Data;
using RedditSentimentTrader.Api.Repositories;

namespace RedditSentimentTrader.Api.Services
{
    public class RedditPostService : IRedditPostService
    {
        private readonly IRedditPostRepository _repository;

        public RedditPostService(IRedditPostRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RedditPost>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<RedditPost> CreateAsync(RedditPost post)
        {
            return await _repository.AddAsync(post);
        }
    }
}
