using System.Text.Json;
using OpenAI.Chat;

namespace RedditSentimentTrader.Api.Models
{
    public interface ISentimentModel
    {
        Task<SentimentResult> ScoreAsync(string text);
    }

    public record SentimentResult(
        string Label,
        double Score,
        double Confidence
    );

    public class OpenAiSentimentModel : ISentimentModel
    {
        private readonly ChatClient _chat;

        public OpenAiSentimentModel(ChatClient chat)
        {
            _chat = chat;
        }

        public Task<SentimentResult> ScoreAsync(string text)
        {
            var messages = new ChatMessage[]
            {
                new SystemChatMessage(
                    "You are a financial sentiment analyzer for stock market comments. " +
                    "Classify sentiment as Bullish, Bearish, or Neutral and give a numeric score between -1 and 1 " +
                    "and a confidence between 0 and 1."
                ),
                new UserChatMessage(
                    $"Return ONLY compact JSON of the form " +
                    "{\"label\":\"Bullish|Bearish|Neutral\",\"score\":-1..1,\"confidence\":0..1} " +
                    $"for this comment: {text}"
                )
            };

            ChatCompletion completion = _chat.CompleteChat(messages);
            var raw = completion.Content[0].Text;

            var result = JsonSerializer.Deserialize<SentimentResult>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? throw new InvalidOperationException("Failed to parse sentiment JSON from model.");

            return Task.FromResult(result);
        }
    }
}
