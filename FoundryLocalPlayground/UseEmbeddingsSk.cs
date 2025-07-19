using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Spectre.Console;

namespace FoundryLocalPlayground;

public class UseEmbeddingsSk : IUse
{
    private readonly string _alias = "qwen2.5-0.5b";
    private readonly string _embeddModelPath = "c:\\LlmCache\\jina-embeddings-v2-base-en\\model.onnx";
    private readonly string _embedVocab = "c:\\LlmCache\\jina-embeddings-v2-base-en\\vocab.txt";
    // port 32770 et 32771 sont les ports par défaut de Qdrant dans Foundry Local avec mon Docker
    private readonly string _qDrantGrpcPort = "32771";

    private readonly string _docPath = "C:\\LlmCache\\jina-embeddings-v2-base-en\\doc.txt";
    private readonly string _docId = "4";


    public async Task ExecuteAsync()
    {
        AnsiConsole.MarkupLine("[green]Initialisation de Foundry Local[/]");
        var manager = await FoundryLocalManager.StartModelAsync(_alias);
        var model = await manager.GetModelInfoAsync(_alias);
        if (model == null) throw new ArgumentException("Model non trouvé");

        AnsiConsole.MarkupLine("[green]Initialisation de Semantic Kernel[/]");

        // voir l'exemple NoteBook: https://github.com/microsoft/Foundry-Local/tree/main/samples/dotNET/rag
        var kernel = Kernel.CreateBuilder()
            .AddBertOnnxEmbeddingGenerator(_embeddModelPath, _embedVocab)
            .AddOpenAIChatCompletion(model.ModelId, endpoint: manager.Endpoint, apiKey: manager.ApiKey, serviceId: model.ModelId)
            .Build();

        AnsiConsole.MarkupLine("[green]Initialisation de qDrant[/]");
        var vectorStoreService = new VectorStoreService(
            $"http://localhost:{_qDrantGrpcPort}",
            "demodocs");
        await vectorStoreService.InitializeAsync();

        AnsiConsole.MarkupLine("[green]Ingestion...[/]");
        var embeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var documentIngestionService = new DocumentIngestionService(embeddingService, vectorStoreService);
        

        // ingestion du document
        await documentIngestionService.IngestDocumentAsync(_docPath, _docId);

        AnsiConsole.MarkupLine("[green]Interrogation[/]");
        // interrogation du document
        var question = "Qu'est-ce que Foundry Local?";

        var chatService = kernel.GetRequiredService<IChatCompletionService>(model.ModelId);
        var ragQueryService = new RagQueryService(embeddingService, chatService, vectorStoreService);
        var answer = await ragQueryService.QueryAsync(question);

        Console.WriteLine($"Question: {question}");
        Console.WriteLine($"Réponse: {answer}");
    }
}

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

public class RagQueryService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingService,
    IChatCompletionService chatService,
    VectorStoreService vectorStoreService)
{
    public async Task<string> QueryAsync(string question)
    {
        var queryEmbeddingResult = await embeddingService.GenerateAsync(question);
        var searchResults = await vectorStoreService.SearchAsync(queryEmbeddingResult.Vector, limit: 5);

        var context = string.Empty;
        foreach (var result in searchResults)
        {
            if (result.Payload.TryGetValue("text", out var text))
            {
                context += text.ToString();
            }
        }
        var prompt = $"According to the question {question}, optimize and simplify the content. {context}";


        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful assistant that answers questions based on the provided context. Answer in french");
        chatHistory.AddUserMessage(prompt);

        var fullMessage = string.Empty;

        await foreach (var chatUpdate in chatService.GetStreamingChatMessageContentsAsync(chatHistory))
        {
            if (chatUpdate.Content is { Length: > 0 })
            {
                fullMessage += chatUpdate.Content;
            }
        }
        return fullMessage;
    }
}

public class DocumentIngestionService(IEmbeddingGenerator<string, Embedding<float>> embeddingService, VectorStoreService vectorStoreService)
{
    public async Task IngestDocumentAsync(string documentPath, string documentId)
    {
        var content = await File.ReadAllTextAsync(documentPath);
        var chunks = ChunkText(content, 300, 60);

        var increment = 100 / chunks.Count;
        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new PercentageColumn(), new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    // Define tasks
                    var task = ctx.AddTask("[yellow]Chargement du document[/]");

                    for (var i = 0; i < chunks.Count; i++)
                    {
                        var chunk = chunks[i];
                        var embeddingResult = await embeddingService.GenerateAsync(chunk);

                        await vectorStoreService.UpsertAsync(
                            embedding: embeddingResult.Vector,
                            metadata: new Dictionary<string, object>
                            {
                                ["document_id"] = documentId,
                                ["chunk_index"] = i,
                                ["text"] = chunk,
                                ["document_path"] = documentPath
                            }
                        );
                        task.Increment(increment);
                    }
                });      
       
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < words.Length; i += chunkSize - overlap)
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