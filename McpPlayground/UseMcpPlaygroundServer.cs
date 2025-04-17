using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Serilog;
using Spectre.Console;

namespace McpPlayground;

public static class UseMcpPlaygroundServer
{
    public static async Task ExecuteAsync()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
            builder.SetMinimumLevel(LogLevel.Trace); 
        });

        var transportOptions = new StdioClientTransportOptions
        {
            Name = "myserver",
            Command = "dotnet",
            Arguments = ["run", "--project", @"..\..\..\..\McpPlaygroundServer", "--no-build"]
        };

        await using var client = await McpClientFactory.CreateAsync(new StdioClientTransport(transportOptions), null, loggerFactory);

        AnsiConsole.MarkupLine("[yellow]Liste des outils du serveur[/]");
        foreach (var tool in await client.ListToolsAsync())
        {
            AnsiConsole.MarkupLine($"    {tool.Name} ({tool.Description})");
        }

        var selectedTool = "Echo";
        AnsiConsole.MarkupLine($"[yellow]Appel de l'outil {selectedTool}[/]");

        while (true)
        {
            var message = AnsiConsole.Prompt(
                new TextPrompt<string>("Quelle est votre question ? ('bye' pour terminer)"));

            if (string.Equals("bye", message, StringComparison.InvariantCultureIgnoreCase))
                break;

            
            var toolArgs = new Dictionary<string, object?> { ["message"] = message };

            var result = await client.CallToolAsync(selectedTool, toolArgs);

            var response = result.Content.First(c => c.Type == "text")?.Text;
            AnsiConsole.MarkupLine(result.IsError ?
                $"[red]Erreur : {response}[/]" : 
                $"[green]Réponse : {response}[/]");
        }
    }
}