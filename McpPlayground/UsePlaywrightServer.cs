using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Spectre.Console;

namespace McpPlayground;

public static class UsePlaywrightServer
{
    public static async Task ExecuteAsync()
    {
        var client = await McpClientFactory.CreateAsync(new()
        {
            Id = "playwright",
            Name = "playwright",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new()
            {
                ["command"] = "npx",
                ["arguments"] = "-y @playwright/mcp@latest --browser msedge --headless"
            }
        });

        AnsiConsole.MarkupLine("[yellow]Liste des outils du serveur[/]");
        foreach (var tool in await client.ListToolsAsync())
        {
            AnsiConsole.MarkupLine($"    {tool.Name} ({tool.Description})");
        }

        AnsiConsole.MarkupLine("[yellow]Appel de l'outil browser_navigate[/]");
        var result = await client.CallToolAsync(
            "browser_navigate",
            new Dictionary<string, object?> { ["url"] = "https://github.com/microsoft/playwright-mcp" },
            CancellationToken.None);
        var response = result.Content.First(c => c.Type == "text")?.Text;
        AnsiConsole.WriteLine(response ?? "???");

        await client.DisposeAsync();
    }
}