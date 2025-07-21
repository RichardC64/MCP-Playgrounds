using System.ClientModel;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using Spectre.Console;

namespace FoundryLocalPlayground;

public class UseEmbeddingsMs : IUse
{
    private readonly string _alias = "qwen2.5-14b";
   private readonly string _embeddModelPath = "C:\\LlmCache\\jinaaijina-embeddings-v3\\model.onnx";
   private readonly string _embedVocab = "C:\\LlmCache\\jinaaijina-embeddings-v3\\tokenizer.json";
    // port 32770 et 32771 sont les ports par défaut de Qdrant dans Foundry Local avec mon Docker
    private readonly string _qDrantGrpcPort = "32771";

    private readonly string _docPath = "C:\\LlmCache\\jinaaijina-embeddings-v3\\doc.txt";
    private readonly string _docId = "5";


    public async Task ExecuteAsync()
    {
        AnsiConsole.MarkupLine("[green]Initialisation de Foundry Local[/]");
        var manager = await FoundryLocalManager.StartModelAsync(_alias);
        var model = await manager.GetModelInfoAsync(_alias);
        if (model == null) throw new ArgumentException("Model non trouvé");

        AnsiConsole.MarkupLine("[green]Initialisation de Semantic Kernel[/]");


        var client = new OpenAIClient(
            new ApiKeyCredential(manager.ApiKey),
            new OpenAIClientOptions { Endpoint = manager.Endpoint });
       var generator = client.GetEmbeddingClient(model.ModelId).AsIEmbeddingGenerator();
    
       var embedding = await generator.GenerateAsync("test");
       

       
        AnsiConsole.MarkupLine("[green]Initialisation de qDrant[/]");
        var vectorStoreService = new VectorStoreService(
            $"http://localhost:{_qDrantGrpcPort}",
            "demodocs");
        await vectorStoreService.InitializeAsync();

    }
}