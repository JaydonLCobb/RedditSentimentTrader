using System.Text.Json;
using System.Text.RegularExpressions;

namespace RedditSentimentTrader.Api.Services
{
    public interface IWsbDailyService
    {
        Task<JsonElement?> FindTodayDiscussionThreadAsync();
        Task<JsonElement?> FindWeekendThreadAsync();
    }

    public class WsbDailyService : IWsbDailyService
    {
        private readonly IRedditApiService _reddit;

        public WsbDailyService(IRedditApiService reddit)
        {
            _reddit = reddit;
        }

        private static readonly Regex DailyPattern = new(
            @"daily\s+discussion\s+thread\s+for\s+([a-zA-Z]+)\s+(\d{1,2}),\s+(\d{4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex WeekendPattern = new(
            @"weekend\s+discussion\s+thread\s+for\s+the\s+weekend\s+of\s+([a-zA-Z]+)\s+(\d{1,2}),\s+(\d{4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public async Task<JsonElement?> FindTodayDiscussionThreadAsync()
        {
            var now = DateTime.UtcNow;

            var posts = await _reddit.GetSubredditPostsAsync("wallstreetbets", "new");
            var arr = posts.GetProperty("data").GetProperty("children");

            foreach (var item in arr.EnumerateArray())
            {
                var title = item.GetProperty("data").GetProperty("title").GetString() ?? "";
                var match = DailyPattern.Match(title);

                if (!match.Success)
                    continue;

                var month = match.Groups[1].Value;
                var day = int.Parse(match.Groups[2].Value);
                var year = int.Parse(match.Groups[3].Value);

                var parsedDate = DateTime.Parse($"{month} {day} {year}");

                if (parsedDate.Date == now.Date)
                    return item.GetProperty("data").Clone();
            }

            return null;
        }

        public async Task<JsonElement?> FindWeekendThreadAsync()
        {
            var posts = await _reddit.GetSubredditPostsAsync("wallstreetbets", "new");
            var arr = posts.GetProperty("data").GetProperty("children");

            foreach (var item in arr.EnumerateArray())
            {
                var title = item.GetProperty("data").GetProperty("title").GetString() ?? "";
                var match = WeekendPattern.Match(title);

                if (match.Success)
                    return item.GetProperty("data").Clone();
            }

            return null;
        }
    }
}
