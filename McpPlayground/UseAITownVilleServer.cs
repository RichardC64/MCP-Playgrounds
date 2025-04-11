using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Spectre.Console;

namespace McpPlayground;

public static class UseAiTownVilleServer
{
    public static async Task ExecuteAsync()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(); // Log to console
            builder.SetMinimumLevel(LogLevel.Trace); // Set minimum log level
        });

        using IChatClient ollamaClient = (new OllamaChatClient("http://localhost:11434/", "llama3.1"));

        using var client = new ChatClientBuilder(ollamaClient)
            .UseFunctionInvocation()
            .UseLogging(loggerFactory)
            .Build();

        
        var tvClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "everything",
            Command = @"..\..\..\..\McpPlaygroundServer\bin\Debug\net9.0\McpPlaygroundServer.exe"
        }));


        var tools = await tvClient.ListToolsAsync().ConfigureAwait(false);

        AnsiConsole.MarkupLine("[yellow]Liste des outils du serveur[/]");
        foreach (var tool in await tvClient.ListToolsAsync())
        {
            AnsiConsole.MarkupLine($"    {tool.Name} ({tool.Description})");
        }

        while (true)
        {
            var prompt = AnsiConsole.Prompt(
                new TextPrompt<string>("Votre question ? ('bye' pour terminer)"));

            if (string.Equals("bye", prompt, StringComparison.InvariantCultureIgnoreCase))
                break;
            // jouer avec la température
            var result = await client.GetResponseAsync(prompt, new()
            {
                Tools = [.. tools],
                Temperature = (float?)0.5
            });

            AnsiConsole.MarkupLine($"\n\n[yellow]{prompt}\n{result}[/]");
        }

        await tvClient.DisposeAsync();
        

    }
}