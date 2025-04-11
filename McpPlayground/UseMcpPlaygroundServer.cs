using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Spectre.Console;

namespace McpPlayground;

public static class UseMcpPlaygroundServer
{
    public static async Task ExecuteAsync()
    {
        var client = await McpClientFactory.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "everything",
            Command = @"..\..\..\..\McpPlaygroundServer\bin\Debug\net9.0\McpPlaygroundServer.exe",

        }));

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