using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace FoundryLocalPlayground;

public class VectorStoreService(string endpoint, string collectionName)
{
    private readonly QdrantClient _client = new(new Uri(endpoint));

    public async Task InitializeAsync(int vectorSize = 768)
    {
        try
        {
            await _client.GetCollectionInfoAsync(collectionName);
        }
        catch
        {
            await _client.CreateCollectionAsync(collectionName, new VectorParams
            {
                Size = (ulong)vectorSize,
                Distance = Distance.Cosine
            });
        }
    }

    public async Task UpsertAsync(ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = Guid.NewGuid().ToString() },
            Vectors = embedding.ToArray()
        };

        foreach (var kvp in metadata)
        {
            point.Payload[kvp.Key] = kvp.Value switch
            {
                string s => s,
                int i => i,
                bool b => b,
                _ => kvp.Value.ToString() ?? string.Empty
            };
        }

        await _client.UpsertAsync(collectionName, [point]);
    }

    public async Task<List<ScoredPoint>> SearchAsync(ReadOnlyMemory<float> queryEmbedding, int limit = 3)
    {
        var searchResult = await _client.SearchAsync(collectionName, queryEmbedding.ToArray(), limit: (ulong)limit);
        return searchResult.ToList();
    }
}