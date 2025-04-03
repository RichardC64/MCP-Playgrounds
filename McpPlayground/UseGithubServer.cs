using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Types;
using Spectre.Console;

namespace McpPlayground;

public static class UseGithubServer
{
    public static async Task ExecuteAsync()
    {
        // Create an MCPClient for the GitHub server
        await using var client = await McpClientFactory.CreateAsync(
            new McpServerConfig()
            {
                Id = "github",
                Name = "GitHub",
                TransportType = "stdio",
                TransportOptions = new Dictionary<string, string>
                {
                    ["command"] = "npx",
                    ["arguments"] = "-y @modelcontextprotocol/server-github",
                }
            },
            new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "GitHub",
                    Version = "1.0.0"
                }
            }).ConfigureAwait(false);

        AnsiConsole.MarkupLine("[yellow]Liste des outils du serveur[/]");
        foreach (var tool in await client.ListToolsAsync())
        {
            AnsiConsole.MarkupLine($"    {tool.Name} ({tool.Description})");
        }

        AnsiConsole.MarkupLine("[yellow]Appel de search_repositories[/]");
        var result = await client.CallToolAsync(
            "search_repositories",
            new Dictionary<string, object?> { ["query"] = "microsoft/semantic-kernel language:csharp sdk" },
            CancellationToken.None);
        var response = result.Content.First(c => c.Type == "text")?.Text;
        AnsiConsole.WriteLine(response ?? "???");

    }
}