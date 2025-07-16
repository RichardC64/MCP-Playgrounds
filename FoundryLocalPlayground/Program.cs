using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using OpenAI;
using Spectre.Console;
using System.ClientModel;
using FoundryLocalPlayground;

var alias = "mistral-7b-v0.2";
var iterations = new[] { 0, 1, 2, 3 };

var manager = await FoundryLocalManager.StartModelAsync(alias);

#region Infos sur les modèles du cache
var cacheFolder = await manager.GetCacheLocationAsync();
var table = new Table
        { Title = new TableTitle($"Modèles dans le cache ({cacheFolder})", new Style(Color.Green)) }
    .AddColumn(new TableColumn("[yellow]Alias[/]"))
    .AddColumn(new TableColumn("[yellow]DisplayName[/]"))
    .AddColumn(new TableColumn("[yellow]File Size[/]"))
    .AddColumn(new TableColumn("[yellow]Tools[/]"))
    .AddColumn(new TableColumn("[yellow]Licence[/]"));

var models = await manager.ListCachedModelsAsync();
foreach (var modelInfo in models)
    table.AddRow(modelInfo.Alias, modelInfo.DisplayName, modelInfo.FileSizeMb.ToString(), modelInfo.SupportsToolCalling.ToString(), modelInfo.License);
AnsiConsole.Write(table);
#endregion

#region Info sur les paramètres de l'API
var model = await manager.GetModelInfoAsync(alias);
if (model == null) throw new ArgumentException("Model non trouvé");

table = new Table
        { Title = new TableTitle("OpenAPI API infos", new Style(Color.Green)) }
    .AddColumn(new TableColumn("[yellow]API Key[/]"))
    .AddColumn(new TableColumn("[yellow]EndPoint[/]"));
table.AddRow(manager.ApiKey, manager.Endpoint.ToString());
AnsiConsole.Write(table);

AnsiConsole.Write(new Rule().RuleStyle("darkgreen"));
#endregion

#region Création du client de chat
var topic = "Ecris un poème de 15 alexandrin sur le Pays Basque en français";
var systemPrompt = "Tu génères un texte basé sur la demande de l'utilisateur. Réponds et français uniquement avec le contenu généré sans commentaires superflus.";
var userPrompt = "Génère le texte basé sur la demande suivante : " + topic;

// choix du client de chat
var selectedClient = AnsiConsole.Prompt(
    new SelectionPrompt<ChatClientType>()
        .Title("[green]Choisissez le type de client de chat :[/]")
        .AddChoices(Enum.GetValues<ChatClientType>()));

AnsiConsole.MarkupLine($"[green]Excellent choix ! Vous avez choisi : {selectedClient}[/]");

await Parallel.ForEachAsync(iterations, async (i, _) =>
{
    var cts = new CancellationTokenSource();
    var chatClient = GetChatClient(selectedClient);
    await foreach (var messagePart in chatClient.GetStreamingResponseAsync(
                       [
                           new ChatMessage(ChatRole.System, systemPrompt),
                           new ChatMessage(ChatRole.User, userPrompt)
                       ], new ChatOptions
                       {
                           MaxOutputTokens = 1000
                       },
                       cts.Token))
    {
        var color = i switch
        {
            0 => "blue",
            1 => "yellow",
            2 => "green",
            _ => "red"
        };
        AnsiConsole.Markup($"[{color}]{messagePart}[/]");
    }
});

#endregion

return;

IChatClient GetChatClient(ChatClientType clientType)
{
    switch (clientType)
    {
        case ChatClientType.FoundryLocal:

            return new OpenAIClient(
                    new ApiKeyCredential(manager.ApiKey),
                    new OpenAIClientOptions { Endpoint = manager.Endpoint })
                .GetChatClient(model.ModelId)
                .AsIChatClient();
        case ChatClientType.Ollama:
            return new OllamaChatClient("http://localhost:11434/", "mistral").AsBuilder().Build();
        default:
            throw new ArgumentOutOfRangeException(nameof(clientType), "ClientType inconnu");
    }
}