namespace RedditSentimentTrader.Api.Options
{
    public class RedditOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string UserAgent { get; set; } = "SentimentTrader/1.0 by u/whosmirage";
        public string RedirectUri { get; set; } = "http://localhost:5265/auth/reddit/callback";
        public string Scope { get; set; } = "read";
    }
}
