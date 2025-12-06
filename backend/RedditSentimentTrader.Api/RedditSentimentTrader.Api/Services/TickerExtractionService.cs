using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace RedditSentimentTrader.Api.Services
{
    public interface ITickerExtractionService
    {
        Task<TickerExtractionResult> ExtractAsync(string text);
    }

    public record TickerExtractionResult(
        bool IsMarketRelated,
        string? PrimaryTicker,
        List<string> Tickers
    );

    internal sealed class TickerExtractionDto
    {
        [JsonPropertyName("is_market_related")]
        public bool IsMarketRelated { get; set; }

        [JsonPropertyName("primary_ticker")]
        public string? PrimaryTicker { get; set; }

        [JsonPropertyName("tickers")]
        public List<string> Tickers { get; set; } = new();
    }

    public class OpenAiTickerExtractionService : ITickerExtractionService
    {
        private readonly ChatClient _chat;

        public OpenAiTickerExtractionService(ChatClient chat)
        {
            _chat = chat;
        }

        public async Task<TickerExtractionResult> ExtractAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TickerExtractionResult(
                    IsMarketRelated: false,
                    PrimaryTicker: null,
                    Tickers: new List<string>()
                );
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "You extract stock tickers from casual Reddit comments. " +
                    "Map company names or nicknames to their primary US stock ticker. " +
                    "Examples:\n" +
                    "\"NVIDIA to the moon\" -> NVDA\n" +
                    "\"buy Google\" -> GOOGL\n" +
                    "\"Meta calls\" -> META\n" +
                    "\"apple and tesla\" -> AAPL, TSLA\n" +
                    "If the comment is not about markets or no ticker is clear, " +
                    "set is_market_related=false, primary_ticker=null and tickers=[]. " +
                    "Always respond with ONLY JSON that matches the given schema."
                ),
                new UserChatMessage($"Comment: \"{text}\"")
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "ticker_extraction",
                    jsonSchema: BinaryData.FromString("""
                    {
                      "type": "object",
                      "properties": {
                        "is_market_related": { "type": "boolean" },
                        "primary_ticker": {
                          "type": ["string","null"],
                          "description": "Main ticker this comment is about, e.g., NVDA, TSLA"
                        },
                        "tickers": {
                          "type": "array",
                          "items": { "type": "string" }
                        }
                      },
                      "required": ["is_market_related","primary_ticker","tickers"],
                      "additionalProperties": false
                    }
                    """),
                    jsonSchemaIsStrict: true)
            };

            ChatCompletion completion = await _chat.CompleteChatAsync(messages, options);

            var rawJson = completion.Content[0].Text?.Trim();

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                // Fallback: treat as not market-related
                return new TickerExtractionResult(false, null, new List<string>());
            }

            TickerExtractionDto dto;
            try
            {
                dto = JsonSerializer.Deserialize<TickerExtractionDto>(
                    rawJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new TickerExtractionDto();
            }
            catch (JsonException)
            {
                // If the model ever sends invalid JSON, don't blow up ingestion
                return new TickerExtractionResult(false, null, new List<string>());
            }

            // Normalize tickers (strip $, upper, map common names)
            string? primary = NormalizeTicker(dto.PrimaryTicker);

            var all = dto.Tickers
                .Select(NormalizeTicker)
                .Where(t => t is not null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()!;

            // If the model says it's market related but gave no usable tickers,
            // we can choose to downgrade it to false.
            bool isMarketRelated = dto.IsMarketRelated && (primary is not null || all.Count > 0);

            return new TickerExtractionResult(isMarketRelated, primary, all);
        }

        private static string? NormalizeTicker(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var t = raw.Trim();

            // strip leading $ / # etc
            if (t.StartsWith("$") || t.StartsWith("#"))
                t = t[1..];

            t = t.Trim().ToUpperInvariant();

            // map common names -> tickers
            return t switch
            {
                "NVIDIA" => "NVDA",
                "NVDA" => "NVDA",

                "GOOGLE" => "GOOGL",
                "ALPHABET" => "GOOGL",

                "FACEBOOK" => "META",
                "META PLATFORMS" => "META",

                "TESLA" => "TSLA",
                "APPLE" => "AAPL",

                _ => t // fallback: assume it's already a ticker
            };
        }
    }
}
