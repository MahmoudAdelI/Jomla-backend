using Google.Protobuf.Collections;
using Jomla.Application.Common.Constants;
using Jomla.Application.Common.Interfaces;
using Jomla.Domain;
using Jomla.Domain.Entities;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Jomla.Infrastructure.AI
{
    public class NegotiationRoundIndexer(
    QdrantClient qdrantClient,
    ITextEmbeddingGenerationService embeddingService) : INegotiationRoundIndexer
    {
        private readonly QdrantClient _qdrantClient = qdrantClient;
        private readonly ITextEmbeddingGenerationService _embeddingService = embeddingService;

        public async Task IndexAsync(GroupRequestOffer offer, string categoryName, int totalParticipants)
        {
            // 1. compute rejection rate — rejected responses / total participants in the group request
            var rejectedCount = offer.Responses
                .Count(r => r.Response == BuyerOfferResponseType.Rejected);
            var rejectionRate = totalParticipants > 0
                ? (float)rejectedCount / totalParticipants
                : 0f;

            // 2. compute discount step percentage relative to opening price
            //    meaningful only for Countered rounds; 0 for Accepted/Expired
            var discountStepPct = offer.UnitPrice > 0
                ? (float)((offer.UnitPrice - offer.CurrentUnitPrice) / offer.UnitPrice)
                : 0f;

            // 3. build embedding text — "{category} — {title} — quantity {qty}"
            var embeddingText =
                $"{categoryName} — {offer.GroupRequest.Title} — quantity {offer.QuantityAvailable}";

            // 4. generate vector
            var vector = await _embeddingService.GenerateEmbeddingAsync(embeddingText);

            // 5. build payload
            var payload = new MapField<string, Value>
            {
                ["offer_id"] = new Value { StringValue = offer.Id.ToString() },
                ["group_request_id"] = new Value { StringValue = offer.GroupRequestId.ToString() },
                ["category_id"] = new Value { StringValue = offer.GroupRequest.CategoryId.ToString() },
                ["category_name"] = new Value { StringValue = categoryName },
                ["round_number"] = new Value { IntegerValue = offer.RoundNumber },
                ["unit_price"] = new Value { DoubleValue = (double)offer.UnitPrice },
                ["min_unit_price"] = new Value { DoubleValue = (double)(offer.MinUnitPrice ?? offer.UnitPrice) },
                ["current_unit_price"] = new Value { DoubleValue = (double)offer.CurrentUnitPrice },
                ["quantity_available"] = new Value { IntegerValue = offer.QuantityAvailable },
                ["rejection_rate"] = new Value { DoubleValue = rejectionRate },
                ["discount_step_pct"] = new Value { DoubleValue = discountStepPct },
                ["status"] = new Value { StringValue = offer.Status.ToString() }
            };

            // 6. upsert point — use offer.Id as the point ID (deterministic, no duplicates on retry)
            await _qdrantClient.UpsertAsync(QdrantCollections.NegotiationRounds, [
                new PointStruct
            {
                Id      = new PointId { Uuid = offer.Id.ToString() },
                Vectors = new Vectors { Vector = new Vector { Data = { vector.ToArray() } } },
                Payload = { payload }
            }
            ]);
        }
    }
}
