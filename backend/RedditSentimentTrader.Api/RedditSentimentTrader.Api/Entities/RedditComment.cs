public class RedditComment
{
    public int Id { get; set; }
    public string RedditId { get; set; } = "";
    public string Author { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string ThreadUrl { get; set; } = "";
    public string? SentimentLabel { get; set; }
    public double? SentimentScore { get; set; }
    public double? Confidence { get; set; }
}

