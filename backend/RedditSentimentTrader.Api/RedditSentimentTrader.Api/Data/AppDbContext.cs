using Microsoft.EntityFrameworkCore;

namespace RedditSentimentTrader.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<RedditPost> RedditPosts { get; set; }
    }
}

