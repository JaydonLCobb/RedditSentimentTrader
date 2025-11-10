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

        public async Task<IEnumerable<RedditPost>> GetAllAsync()
        {
            return await _context.RedditPosts.ToListAsync();
        }

        public async Task<RedditPost?> GetByIdAsync(int id)
        {
            return await _context.RedditPosts.FindAsync(id);
        }

        public async Task<RedditPost> CreateAsync(RedditPost post)
        {
            _context.RedditPosts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<RedditPost?> UpdateAsync(RedditPost post)
        {
            var existing = await _context.RedditPosts.FindAsync(post.Id);
            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(post);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.RedditPosts.FindAsync(id);
            if (existing == null)
                return false;

            _context.RedditPosts.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
