using Jomla.Application.Common.Constants;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text.Json;

namespace Jomla.Infrastructure.AI
{
    public class NegotiationAgent(
        IChatCompletionService chatService,
        ITextEmbeddingGenerationService embeddingService,
        QdrantClient qdrantClient,
        ILogger<NegotiationAgent> logger) : INegotiationAgent
    {
        private readonly IChatCompletionService _chatService = chatService;
        private readonly ITextEmbeddingGenerationService _embeddingService = embeddingService;
        private readonly QdrantClient _qdrantClient = qdrantClient;
        private readonly ILogger<NegotiationAgent> _logger = logger;

        private const int TopK = 5;
        private const int MaxNegotiationRounds = 4;
        public async Task<decimal> GetNextPriceAsync(GroupRequestOffer offer, string categoryName)
        {
            _logger.LogInformation("NegotiationAgent starting price recommendation for Offer {OfferId} (Category: {CategoryName}). CurrentPrice: {CurrentPrice}, Floor: {Floor}.", 
                offer.Id, categoryName, offer.CurrentUnitPrice, offer.MinUnitPrice ?? offer.UnitPrice);

            var floor = offer.MinUnitPrice ?? offer.UnitPrice;

            if (offer.RoundNumber >= MaxNegotiationRounds)
            {
                _logger.LogInformation("Offer {OfferId} has reached or exceeded max negotiation rounds ({MaxRounds}). Dropping directly to floor price {Floor}.", 
                    offer.Id, MaxNegotiationRounds, floor);
                return floor;
            }

            // 1. build embedding text for similarity search
            var embeddingText =
                $"{categoryName} — {offer.GroupRequest.Title} — quantity {offer.QuantityAvailable}";

            // 2. generate vector and query Qdrant for similar past rounds
            var vector = await _embeddingService.GenerateEmbeddingAsync(embeddingText);

            var searchResults = await _qdrantClient.SearchAsync(
            collectionName: QdrantCollections.NegotiationRounds,
            vector: vector.ToArray(),
            filter: new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "category_id",
                            Match = new Match
                            {
                                Keyword = offer.GroupRequest.CategoryId.ToString()
                            }
                        }
                    }
                },
                MustNot =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "group_request_id",
                            Match = new Match
                            {
                                Keyword = offer.GroupRequestId.ToString()
                            }
                        }
                    }
                }
            },
            limit: TopK);

            // 3. fallback if no useful results — fixed 25% step
            if (searchResults.Count == 0)
            {
                _logger.LogInformation("No similar negotiation rounds found in Qdrant for Category: {CategoryName}. Using fallback pricing.", categoryName);
                var fallbackPrice = ComputeFallbackPrice(offer, floor);
                _logger.LogInformation("Fallback price calculated: {FallbackPrice}.", fallbackPrice);
                return fallbackPrice;
            }

            _logger.LogInformation("Found {Count} similar negotiation rounds in Qdrant. Building RAG context...", searchResults.Count);

            // 4. build RAG context from retrieved rounds
            var ragContext = string.Join("\n", searchResults.Select(r =>
                $"- Round {r.Payload["round_number"].IntegerValue}: " +
                $"price {r.Payload["current_unit_price"].DoubleValue}, " +
                $"min {r.Payload["min_unit_price"].DoubleValue}, " +
                $"discount step {r.Payload["discount_step_pct"].DoubleValue:P0}, " +
                $"rejection rate {r.Payload["rejection_rate"].DoubleValue:P0}, " +
                $"status {r.Payload["status"].StringValue}"));

            _logger.LogDebug("RAG Context:\n{RagContext}", ragContext);

            // 5. build prompt
            var history = new ChatHistory();
            var systemMessage = @"""
                You are a pricing negotiation agent for a B2B group-buying marketplace.
                Your job is to recommend the next unit price for a supplier offer that failed to fill before expiry.
                You must never go below the minimum unit price floor.
                Respond ONLY with a valid JSON object in this exact format: {""new_price"": 123.45}
                """;
            history.AddSystemMessage(systemMessage);

            var userMessage = $"""
                Current offer details:
                - Category: {categoryName}
                - Item: {offer.GroupRequest.Title}
                - Opening price: {offer.UnitPrice}
                - Current price: {offer.CurrentUnitPrice}
                - Minimum floor: {floor}
                - Round number: {offer.RoundNumber}
                - Quantity available: {offer.QuantityAvailable}

                Similar past negotiation rounds for context:
                {ragContext}

                Recommend the next unit price. It must be between {floor} (inclusive) and {offer.CurrentUnitPrice} (exclusive).
                """;
            history.AddUserMessage(userMessage);

            _logger.LogDebug("Sending prompt to LLM. User Message:\n{UserMessage}", userMessage);

            // 6. call LLM
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = "json_object"
            };

            var response = await _chatService.GetChatMessageContentAsync(
                history, executionSettings);

            _logger.LogInformation("Received response from LLM: {ResponseContent}", response.Content);

            // 7. parse response — fallback if parsing fails
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
                var newPrice = json.GetProperty("new_price").GetDecimal();
                var clampedPrice = Math.Max(Math.Min(newPrice, offer.CurrentUnitPrice), floor);

                _logger.LogInformation("Parsed price recommended by LLM: {RecommendedPrice}. Clamped price: {ClampedPrice}.", newPrice, clampedPrice);
                return clampedPrice;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM response. Using fallback pricing.");
                var fallbackPrice = ComputeFallbackPrice(offer, floor);
                _logger.LogInformation("Fallback price calculated: {FallbackPrice}.", fallbackPrice);
                return fallbackPrice;
            }

        }

        // fixed 25% of original price gap, clamped at floor
        private static decimal ComputeFallbackPrice(GroupRequestOffer offer, decimal floor)
        {
            var step = 0.25m * (offer.UnitPrice - floor);
            return Math.Max(offer.CurrentUnitPrice - step, floor);
        }
    }
}
