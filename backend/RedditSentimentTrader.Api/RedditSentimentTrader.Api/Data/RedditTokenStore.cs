using System.ComponentModel.DataAnnotations;

namespace RedditSentimentTrader.Api.Data
{
    public class RedditTokenStore
    {
        [Key]
        public int Id { get; set; } 
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
