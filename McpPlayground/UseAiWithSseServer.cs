using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Serilog;
using Spectre.Console;

namespace McpPlayground;

public static class UseAiWithSseServer
{
    public static async Task ExecuteAsync()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
            builder.SetMinimumLevel(LogLevel.Trace);
        });

#if LOG
        using var httpClient = new HttpClient(new LoggingHandler { InnerHandler = new HttpClientHandler() });
#else
        using var httpClient = new HttpClient();
#endif

        using IChatClient ollamaClient = (new OllamaChatClient("http://localhost:11434/", "llama3.1", httpClient));

        using var client = new ChatClientBuilder(ollamaClient)
            .UseFunctionInvocation()
            .UseLogging(loggerFactory)
            .Build();

        var transportOptions = new SseClientTransportOptions
        {
            Name = "myaspnetServer",
            Endpoint = new Uri("http://localhost:5261/sse"),
           // UseStreamableHttp = true, sera dispo en v12

        };

        var mcpClient = await McpClientFactory.CreateAsync(new SseClientTransport(transportOptions), CreateOptions(), loggerFactory);
        await mcpClient.SetLoggingLevel(LogLevel.Trace);
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

        while (true)
        {
            var prompt = AnsiConsole.Prompt(
                new TextPrompt<string>("Votre question ? ('bye' pour terminer)"));

            if (string.Equals("bye", prompt, StringComparison.InvariantCultureIgnoreCase))
                break;

            var result = await client.GetResponseAsync(prompt, new()
            {
                Tools = [.. tools],
                Temperature = 0
            });

            // Réception des messages de l'IA et des outils
            foreach (var resultMessage in result.Messages)
            {
                if (resultMessage.Role == ChatRole.Tool)
                {
                    AnsiConsole.MarkupLine($"[blue]Réponse de l'outil[/]");
                    if (resultMessage.Contents.FirstOrDefault() is not FunctionResultContent
                        {
                            Result: JsonElement jsonElement
                        }) continue;
                    Console.WriteLine(jsonElement.GetProperty("content").EnumerateArray().FirstOrDefault().GetProperty("text").ToString());
                }
                else if (resultMessage.Role == ChatRole.Assistant)
                {
                    switch (resultMessage.Contents.FirstOrDefault())
                    {
                        case FunctionCallContent functionCallContent:
                            AnsiConsole.MarkupLine("[yellow]Appel de l'outil[/]");
                            Console.WriteLine($"Outil : {functionCallContent.Name}, Argument : {functionCallContent.Arguments?.Values.First()}");
                            break;
                        case TextContent textContent:
                            AnsiConsole.MarkupLine("[yellow]Reformulation par l'IA[/]");
                            Console.WriteLine($"{textContent.Text}");
                            break;
                    }
                }
            }
        }

        await mcpClient.DisposeAsync();
    }

    private static McpClientOptions CreateOptions()
    {
        return new McpClientOptions
        {
            Capabilities = new()
            {
                NotificationHandlers = [
                    new ("notifications/message", (notification, _) =>
                    {
                        var message = notification?.Params?.ToString();
                        AnsiConsole.MarkupLine($"[red]Received notification: {message}[/]");
                        return ValueTask.CompletedTask;
                    })
                ],
                
                
            }
        };
    }
}