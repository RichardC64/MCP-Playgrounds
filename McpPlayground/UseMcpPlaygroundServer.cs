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

        var client = await McpClientFactory.CreateAsync(new StdioClientTransport(transportOptions), null, loggerFactory);

        AnsiConsole.MarkupLine("[yellow]Liste des outils du serveur[/]");
        foreach (var tool in await client.ListToolsAsync())
        {
            AnsiConsole.MarkupLine($"    {tool.Name} ({tool.Description})");
        }

        AnsiConsole.MarkupLine("[yellow]Appel de l'outil Echo[/]");
        var result = await client.CallToolAsync(
            "Echo",
            new Dictionary<string, object?> { ["message"] = "Richard Clark" });
        var response = result.Content.First(c => c.Type == "text")?.Text;
        AnsiConsole.MarkupLine(response ?? "???");


        await client.DisposeAsync();
    }
}


//https://devblogs.microsoft.com/dotnet/dotnet-10-preview-3/