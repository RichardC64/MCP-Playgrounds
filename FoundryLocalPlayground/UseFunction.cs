using System.ClientModel;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using Spectre.Console;

namespace FoundryLocalPlayground;

public class UseFunction : IUse
{
    private readonly string _alias = "phi-4-mini";
    private readonly string _ollamaModelId = "mistral";
    
    public async Task ExecuteAsync()
    {
        var manager = await FoundryLocalManager.StartModelAsync(_alias);
        var model = await manager.GetModelInfoAsync(_alias);
        if (model == null) throw new ArgumentException("Model non trouvé");

        var transportOptions = new StdioClientTransportOptions
        {
            Name = "townVilleServer",
            Command = "dotnet",
            Arguments = ["run", "--project", @"..\..\..\..\McpPlaygroundServer", "--no-build"]
        };
        var tvClient = await McpClientFactory.CreateAsync(new StdioClientTransport(transportOptions));
        var tools = await tvClient.ListToolsAsync().ConfigureAwait(false);

        // choix du client de chat
        var selectedClient = AnsiConsole.Prompt(
            new SelectionPrompt<ChatClientType>()
                .Title("[green]Choisissez le type de client de chat :[/]")
                .AddChoices(Enum.GetValues<ChatClientType>()));

        AnsiConsole.MarkupLine($"[green]Excellent choix ! Vous avez choisi : {selectedClient}[/]");
        

        var chatClient = GetChatClient(selectedClient, manager, model.ModelId);

        var result = await chatClient.GetResponseAsync("Qui est le maire de TownVille ?", new ChatOptions
          {
              Tools = [..tools],
              Temperature = (float?)0,
              MaxOutputTokens = 4096,
              TopP = (float)1.0
          });
          Console.WriteLine($"{result}");

      await tvClient.DisposeAsync();

        #region cleaning
        await manager.UnloadModelAsync(model.ModelId);
        await manager.DisposeAsync();
        #endregion
    }

    private IChatClient GetChatClient(ChatClientType clientType, FoundryLocalManager manager, string modelId)
    {
        switch (clientType)
        {
            case ChatClientType.FoundryLocal:

                return new ChatClientBuilder(
                        new OpenAIClient(
                                new ApiKeyCredential(manager.ApiKey), 
                                new OpenAIClientOptions { Endpoint = manager.Endpoint })
                            .GetChatClient(modelId)
                            .AsIChatClient())
                    .UseFunctionInvocation()
                    .Build();
            case ChatClientType.Ollama:
                return new OllamaChatClient("http://localhost:11434/", _ollamaModelId)
                    .AsBuilder()
                    .UseFunctionInvocation()
                    .Build();
            default:
                throw new ArgumentOutOfRangeException(nameof(clientType), "ClientType inconnu");
        }
    }
}