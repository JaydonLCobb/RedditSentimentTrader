using Microsoft.EntityFrameworkCore;
using RedditSentimentTrader.Api.Data;

namespace RedditSentimentTrader.Api.Repositories
{
    public class RedditPostRepository : IRedditPostRepository
    {
        private readonly AppDbContext _context;
        public RedditPostRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RedditPost>> GetAllAsync()
        {
            return await _context.RedditPosts.ToListAsync();
        }

        public async Task<RedditPost> AddAsync(RedditPost post)
        {
            _context.RedditPosts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }


    }
}
