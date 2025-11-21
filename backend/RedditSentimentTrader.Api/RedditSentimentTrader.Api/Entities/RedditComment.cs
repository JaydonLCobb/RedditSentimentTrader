public class RedditComment
{
    public int Id { get; set; }  
    public string RedditId { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public string ThreadUrl { get; set; } = string.Empty;
}

