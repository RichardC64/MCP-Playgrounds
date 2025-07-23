using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Spectre.Console;

namespace FoundryLocalPlayground;

public class UseEmbeddingsSk : IUse
{
    private readonly string _alias = "qwen2.5-0.5b";
    private readonly string _embeddedFolder = "C:\\python\\onnx\\solon";

    private string EmbeddedModelPath => Path.Combine(_embeddedFolder, "model.onnx");
    private string EmbeddedVocabPath => Path.Combine(_embeddedFolder, "vocab.txt");

    // port 32770 et 32771 sont les ports par défaut de Qdrant dans Foundry Local avec mon Docker
    private readonly string _qDrantGrpcPort = "32771";

    private readonly string _docPath = "C:\\LlmCache\\test.txt";
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
            .AddBertOnnxEmbeddingGenerator(EmbeddedModelPath, EmbeddedVocabPath, new BertOnnxOptions())
            .AddOpenAIChatCompletion(model.ModelId, endpoint: manager.Endpoint, apiKey: manager.ApiKey, serviceId: model.ModelId)
            .Build();

        AnsiConsole.MarkupLine("[green]Initialisation de qDrant[/]");
        var vectorStoreService = new VectorStoreService(
            $"http://localhost:{_qDrantGrpcPort}",
            "demodocs");
        await vectorStoreService.InitializeAsync(119547);

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