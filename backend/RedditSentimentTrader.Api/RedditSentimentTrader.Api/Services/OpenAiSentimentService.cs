using System.Text.Json;
using OpenAI.Chat;

namespace RedditSentimentTrader.Api.Services
{
    public interface ISentimentService
    {
        Task<SentimentResult> ScoreAsync(string text);
    }

    public record SentimentResult(
        string Label,
        double Score,
        double Confidence
    );

    public class OpenAiSentimentService : ISentimentService
    {
        private readonly ChatClient _chat;

        public OpenAiSentimentService(ChatClient chat)
        {
            _chat = chat;
        }

        public async Task<SentimentResult> ScoreAsync(string text)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "You are a financial sentiment analyzer for stock market discussion. " +
                    "Classify comments as bullish / bearish / neutral and give a numeric score."
                ),
                new UserChatMessage(
                    $"Comment: \"{text}\"\n\n" +
                    "Respond ONLY as JSON matching this schema: " +
                    "{ label: string, score: number, confidence: number }"
                )
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "sentiment_response",
                    jsonSchema: BinaryData.FromBytes("""
                    {
                      "type": "object",
                      "properties": {
                        "label": {
                          "type": "string",
                          "description": "One of: bullish, bearish, neutral, other"
                        },
                        "score": {
                          "type": "number",
                          "description": "Sentiment score in [-1,1], bullish>0, bearish<0, neutral≈0"
                        },
                        "confidence": {
                          "type": "number",
                          "minimum": 0,
                          "maximum": 1,
                          "description": "Model confidence in this classification"
                        }
                      },
                      "required": ["label","score","confidence"],
                      "additionalProperties": false
                    }
                    """u8.ToArray()),
                    jsonSchemaIsStrict: true)
            };

            ChatCompletion completion = await _chat.CompleteChatAsync(messages, options);

            string json = completion.Content[0].Text;

            var result = JsonSerializer.Deserialize<SentimentResult>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (result is null)
                throw new InvalidOperationException($"Failed to parse sentiment JSON: {json}");

            return result;
        }
    }
}
