using System;
using System.ComponentModel.DataAnnotations;

namespace RedditSentimentTrader.Api.Data
{
    // Table structure/data
    public class RedditPost
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 1)]
        [RegularExpression(("^[A-Z]+$"))]
        public string Ticker { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Author { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        [Range(-1.0, 1.0)]
        public double SentimentScore { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}

