using Jomla.Application.Common.Constants;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Jomla.Infrastructure.Persistance.Qdrant
{
    public class NegotiationRoundsCollectionInitializer(QdrantClient qdrantClient)
    {
        private readonly QdrantClient _qdrantClient = qdrantClient;

        // text-embedding-3-small output size
        private const ulong VectorSize = 1536;

        public async Task InitializeAsync()
        {
            // check if collection already exists — skip creation if so
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (collections.Any(c => c == QdrantCollections.NegotiationRounds))
                return;

            await _qdrantClient.CreateCollectionAsync(QdrantCollections.NegotiationRounds, new VectorParams
            {
                Size = VectorSize,
                Distance = Distance.Cosine
            });
        }
    }
}
