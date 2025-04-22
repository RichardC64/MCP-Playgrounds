using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Messages;
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
            Endpoint = new Uri("http://localhost:5261/sse")
        };
        var tvClient = await McpClientFactory.CreateAsync(new SseClientTransport(transportOptions), null, loggerFactory);

        await tvClient.SendNotificationAsync("test/notification", new Message{ Content = "Coucou depuis le client"}).ConfigureAwait(false);

        tvClient.RegisterNotificationHandler("test/notification", async (notification, cancellationToken) =>
        {
            var message = notification?.Params?["Content"]?.ToString();
            Console.WriteLine($"Received notification: {message}");
        });

        tvClient.RegisterNotificationHandler(NotificationMethods.LoggingMessageNotification, async (notification, cancellationToken) =>
        {
            var message = notification?.Params?.ToString();
            Console.WriteLine($"Received notification: {message}");
        });

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
                Temperature = (float?)0
            });

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

        await tvClient.DisposeAsync();

    }
}