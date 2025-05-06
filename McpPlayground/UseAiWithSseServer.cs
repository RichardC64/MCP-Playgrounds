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
           // UseStreamableHttp = true, sera dispo en v12, retirer /see au endpoint dans ce cas

        };

        var mcpClient = await McpClientFactory.CreateAsync(new SseClientTransport(transportOptions), CreateOptions(), loggerFactory);

        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
        while (true)
        {
            var prompt = AnsiConsole.Prompt(
                new TextPrompt<string>("Votre question ? ('bye' pour terminer)"));

            if (string.Equals("bye", prompt, StringComparison.InvariantCultureIgnoreCase))
                break;

            var cts = new CancellationTokenSource();
            var task = client.GetStreamingResponseAsync(prompt, new() { Tools = [.. tools] }, cts.Token);
            try
            {
                await foreach (var update in task)
                {
                    if (update.Role == ChatRole.Tool)
                    {
                        WriteToolAnswer(update);
                    }
                    else if (update.Role == ChatRole.Assistant)
                    {
                        switch (update.Contents.FirstOrDefault())
                        {
                            case FunctionCallContent functionCallContent:
                                await ManageToolCall(functionCallContent, update, cts);
                                break;
                            case TextContent textContent:
                                WriteFinalAnswer(textContent);
                                break;
                        }
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine("L'appel à l'outil a été annulé");
            }
        }

        await mcpClient.DisposeAsync();
    }

    private static async Task ManageToolCall(FunctionCallContent functionCallContent, ChatResponseUpdate update,
        CancellationTokenSource cts)
    {
        AnsiConsole.MarkupLine("[yellow]Appel de l'outil[/]");
        // Demander confirmation à l'utilisateur
        var confirmation = AnsiConsole.Confirm(
            $"L'outil {functionCallContent.Name} va être appelé avec l'argument {functionCallContent.Arguments?.Values.First()}. Voulez-vous l'autoriser ?");
        if (!confirmation)
        {
            AnsiConsole.MarkupLine("[red]Appel à l'outil annulé par l'utilisateur.[/]");
            await cts.CancelAsync();
            return;
        }

        AnsiConsole.MarkupLine("[green]Appel à l'outil autorisé.[/]");
    }

    private static void WriteFinalAnswer(TextContent textContent)
    {
        AnsiConsole.MarkupLine("[yellow]Reformulation par l'IA[/]");
        Console.WriteLine($"{textContent.Text}");
    }

    private static void WriteToolAnswer(ChatResponseUpdate update)
    {
        AnsiConsole.MarkupLine($"[blue]Réponse de l'outil[/]");
        if (update.Contents.FirstOrDefault() is not FunctionResultContent
            {
                Result: JsonElement jsonElement
            }) return;
        Console.WriteLine(jsonElement.GetProperty("content").EnumerateArray().FirstOrDefault()
            .GetProperty("text").ToString());
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