using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Serilog;
using Spectre.Console;

namespace McpPlayground;

public static class UseMcpPlaygroundServerComplexDatas
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

        var selectedTool = "Adjoints";
        AnsiConsole.MarkupLine($"[yellow]Appel de l'outil {selectedTool}[/]");

        var result = await client.CallToolAsync(selectedTool);

        var response = result.Content.First(c => c.Type == "text").Text;
        if (result.IsError)
            AnsiConsole.MarkupLine($"[red]Erreur : {response}[/]");
        else
        {
            if (response == null)
            {
                AnsiConsole.MarkupLine($"[red]Erreur : réponse vide[/]");
                return;
            }
            var adjoints = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<Adjoint>>(response);
            if (adjoints == null)
            {
                AnsiConsole.MarkupLine($"[red]Erreur : réponse json invalide[/]");
                return;
            }
            AnsiConsole.MarkupLine($"[green]Liste des adjoints[/]");
            foreach (var adjoint in adjoints)
            {
                AnsiConsole.MarkupLine($"     [yellow]{adjoint.Function} : {adjoint.Name}[/]");
            }
            
        }
    }
    internal record Adjoint(
    [property: JsonPropertyName("name")] string Name, 
    [property: JsonPropertyName("function")] string Function, 
    [property: JsonPropertyName("age")] int Age);
}


