using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace FoundryLocalPlayground;

public class UseEmbeddings : IUse
{
    public async Task ExecuteAsync()
    {
        // voir https://github.com/microsoft/Foundry-Local/tree/main/samples/dotNET/rag
        var builder = Kernel.CreateBuilder();
        var embeddModelPath = "Your Jinaai jina-embeddings-v2-base-en onnx model path";
        var embedVocab = "Your Jinaai ina-embeddings-v2-base-en vocab file path";
        builder.AddBertOnnxEmbeddingGenerator(embeddModelPath, embedVocab);
        builder.AddOpenAIChatCompletion("qwen2.5-0.5b-instruct-generic-gpu", new Uri("http://localhost:5273/v1"), apiKey: "", serviceId: "qwen2.5-0.5b");

        var kernel = builder.Build();

        var chatService = kernel.GetRequiredService<IChatCompletionService>(serviceKey: "qwen2.5-0.5b");
        var embeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        var vectorStoreService = new VectorStoreService(
            "http://localhost:6334",
            "",
            "demodocs");

        await vectorStoreService.InitializeAsync();


        var documentIngestionService = new DocumentIngestionService(embeddingService, vectorStoreService);
        var ragQueryService = new RagQueryService(embeddingService, chatService, vectorStoreService);

        var filePath = "./foundry-local-architecture.md";
        var fileID = "3";

        await documentIngestionService.IngestDocumentAsync(filePath, fileID);

        var question = "What's Foundry Local?";

        var answer = await ragQueryService.QueryAsync(question);

        Console.WriteLine($"Question: {question}");
        Console.WriteLine($"Answer: {answer}");
    }
}

public class VectorStoreService
{
    private readonly QdrantClient _client;
    private readonly string _collectionName;

    public VectorStoreService(string endpoint, string apiKey, string collectionName)
    {
        _client = new QdrantClient(new Uri(endpoint));
        _collectionName = collectionName;
    }

    public async Task InitializeAsync(int vectorSize = 768)
    {
        try
        {
            await _client.GetCollectionInfoAsync(_collectionName);
        }
        catch
        {
            await _client.CreateCollectionAsync(_collectionName, new VectorParams
            {
                Size = (ulong)vectorSize,
                Distance = Distance.Cosine
            });
        }
    }

    public async Task UpsertAsync(string id, ReadOnlyMemory<float> embedding, Dictionary<string, object> metadata)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = id },
            Vectors = embedding.ToArray(),
            Payload = { }
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

        await _client.UpsertAsync(_collectionName, new[] { point });
    }

    public async Task<List<ScoredPoint>> SearchAsync(ReadOnlyMemory<float> queryEmbedding, int limit = 3)
    {
        var searchResult = await _client.SearchAsync(_collectionName, queryEmbedding.ToArray(), limit: (ulong)limit);
        return searchResult.ToList();
    }
}

public class RagQueryService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
    private readonly IChatCompletionService _chatService;
    private readonly VectorStoreService _vectorStoreService;

    public RagQueryService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingService,
        IChatCompletionService chatService,
        VectorStoreService vectorStoreService)
    {
        _embeddingService = embeddingService;
        _chatService = chatService;
        _vectorStoreService = vectorStoreService;
    }

    public async Task<string> QueryAsync(string question)
    {
        // return question; // For now, just return the question as a placeholder
        var queryEmbeddingResult = await _embeddingService.GenerateAsync(question);
        //         Console.WriteLine(question);
        var queryEmbedding = queryEmbeddingResult.Vector;
        var searchResults = await _vectorStoreService.SearchAsync(queryEmbedding, limit: 5);

        string str_context = "";
        foreach (var result in searchResults)
        {
            if (result.Payload.TryGetValue("text", out var text))
            {
                str_context += text.ToString();
            }
        }
        var prompt = $@"According to the question {question},, optimize and simplify the content. {str_context}";


        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful assistant that answers questions based on the provided context.");
        chatHistory.AddUserMessage(prompt);

        var fullMessage = string.Empty;

        await foreach (var chatUpdate in _chatService.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: default))
        {
            if (chatUpdate.Content is { Length: > 0 })
            {
                fullMessage += chatUpdate.Content;
            }
        }
        return fullMessage ?? "I couldn't generate a response.";
    }
}

public class DocumentIngestionService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
    private readonly VectorStoreService _vectorStoreService;

    public DocumentIngestionService(IEmbeddingGenerator<string, Embedding<float>> embeddingService, VectorStoreService vectorStoreService)
    {
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
    }

    public async Task IngestDocumentAsync(string documentPath, string documentId)
    {
        var content = await File.ReadAllTextAsync(documentPath);
        var chunks = ChunkText(content, 300, 60);

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var embeddingResult = await _embeddingService.GenerateAsync(chunk);
            var embedding = embeddingResult.Vector;

            await _vectorStoreService.UpsertAsync(
                id: Guid.NewGuid().ToString(),
                embedding: embedding,
                metadata: new Dictionary<string, object>
                {
                    ["document_id"] = documentId,
                    ["chunk_index"] = i,
                    ["text"] = chunk,
                    ["document_path"] = documentPath
                }
            );
        }
    }

    private List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i += chunkSize - overlap)
        {
            var chunkWords = words.Skip(i).Take(chunkSize).ToArray();
            var chunk = string.Join(" ", chunkWords);
            chunks.Add(chunk);

            if (i + chunkSize >= words.Length)
                break;
        }

        return chunks;
    }
}