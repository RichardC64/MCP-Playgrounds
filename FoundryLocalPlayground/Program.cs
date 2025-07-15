using Microsoft.AI.Foundry.Local;
using OpenAI;
using System.ClientModel;
using OpenAI.Chat;

var alias = "deepseek-r1-7b";

var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: alias);

// Cache
var cacheFolder = await manager.GetCacheLocationAsync();
Console.WriteLine($"Dossier du cache: {cacheFolder}");
Console.WriteLine("Modèles dans le cache:");
var models = await manager.ListCachedModelsAsync();
foreach (var modelInfo in models)
{
    Console.WriteLine($"{modelInfo.Alias}: {modelInfo.DisplayName}, {modelInfo.FileSizeMb}Mb, support Tools: {modelInfo.SupportsToolCalling}");
    foreach (var parameter in modelInfo.ModelSettings.Parameters)
    {
        Console.WriteLine(parameter);
    }


}

var model = await manager.GetModelInfoAsync(aliasOrModelId: alias);
if (model == null)
{
    Console.WriteLine($"Model {alias} not found.");
    return;
}

// création du client http
var key = new ApiKeyCredential(manager.ApiKey);
var client = new OpenAIClient(key, new OpenAIClientOptions
{
    Endpoint = manager.Endpoint
});

Console.WriteLine($"Model: {model.ModelId} - {model.Alias} - {model.FileSizeMb}Mb - Licence: {model.License}");

Console.WriteLine($"Uri: {manager.Endpoint}");
var chatClient = client.GetChatClient(model?.ModelId);

//var chatMessage = new SystemChatMessage("Tu ne parle qu'en français");

var completionUpdates = chatClient.CompleteChatStreaming("Combien font 4 + 5");

Console.Write($"[ASSISTANT]: ");
foreach (var completionUpdate in completionUpdates)
{
    if (completionUpdate.ContentUpdate.Count > 0)
    {
        Console.Write(completionUpdate.ContentUpdate[0].Text);
    }
}

Console.ReadLine();