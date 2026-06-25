using Jomla.Application.Common.Constants;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain.Entities;
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
        QdrantClient qdrantClient) : INegotiationAgent
    {
        private readonly IChatCompletionService _chatService = chatService;
        private readonly ITextEmbeddingGenerationService _embeddingService = embeddingService;
        private readonly QdrantClient _qdrantClient = qdrantClient;

        private const int TopK = 5;
        public async Task<decimal> GetNextPriceAsync(GroupRequestOffer offer, string categoryName)
        {
            var floor = offer.MinUnitPrice ?? offer.UnitPrice;

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
                                Text = offer.GroupRequest.CategoryId.ToString()
                            }
                        }
                    }
                }
            },
            limit: TopK);

            // 3. fallback if no useful results — fixed 25% step
            if (searchResults.Count == 0)
                return ComputeFallbackPrice(offer, floor);

            // 4. build RAG context from retrieved rounds
            var ragContext = string.Join("\n", searchResults.Select(r =>
                $"- Round {r.Payload["round_number"].IntegerValue}: " +
                $"price {r.Payload["current_unit_price"].DoubleValue}, " +
                $"min {r.Payload["min_unit_price"].DoubleValue}, " +
                $"discount step {r.Payload["discount_step_pct"].DoubleValue:P0}, " +
                $"rejection rate {r.Payload["rejection_rate"].DoubleValue:P0}, " +
                $"status {r.Payload["status"].StringValue}"));

            // 5. build prompt
            var history = new ChatHistory();
            var systemMessage = """
                You are a pricing negotiation agent for a B2B group-buying marketplace.
                Your job is to recommend the next unit price for a supplier offer that failed to fill before expiry.
                You must never go below the minimum unit price floor.
                Respond ONLY with a valid JSON object in this exact format: {"new_price": 123.45}
                """;
            history.AddSystemMessage(systemMessage);

            history.AddUserMessage($"""
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

                Recommend the next unit price. It must be between {floor} and {offer.CurrentUnitPrice} (exclusive lower, inclusive upper).
                """);

            // 6. call LLM
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = "json_object"
            };

            var response = await _chatService.GetChatMessageContentAsync(
                history, executionSettings);

            // 7. parse response — fallback if parsing fails
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
                var newPrice = json.GetProperty("new_price").GetDecimal();

                // clamp to valid range
                return Math.Max(Math.Min(newPrice, offer.CurrentUnitPrice), floor);
            }
            catch
            {
                return ComputeFallbackPrice(offer, floor);
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
