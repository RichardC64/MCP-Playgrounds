using System.ClientModel;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

namespace FoundryLocalPlayground;

public class UseFunction : IUse
{
    private readonly string _alias = "phi-4-mini";
    
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
        
        //var chatClient1 = new OpenAIClient(
        //        new ApiKeyCredential(manager.ApiKey),
        //        new OpenAIClientOptions { Endpoint = manager.Endpoint })
        //    .GetChatClient(model.ModelId)
        //    .AsIChatClient();

      var chatClient =  new ChatClientBuilder(
                new OpenAIClient(new ApiKeyCredential(manager.ApiKey), new OpenAIClientOptions { Endpoint = manager.Endpoint })
                    .GetChatClient(model.ModelId).AsIChatClient())
            .UseFunctionInvocation()
            .Build();
        
        var result = await chatClient.GetResponseAsync("Qui est le maire de TownVille", new ChatOptions
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
}