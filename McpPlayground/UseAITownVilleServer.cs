// #define LOG
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Spectre.Console;
using Serilog;

namespace McpPlayground;

public static class UseAiTownVilleServer
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

        var transportOptions = new StdioClientTransportOptions
        {
            Name = "townVilleServer",
            Command = "dotnet",
            Arguments = ["run", "--project", @"..\..\..\..\McpPlaygroundServer", "--no-build"]
        };
        var tvClient = await McpClientFactory.CreateAsync(new StdioClientTransport(transportOptions), null, loggerFactory);


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

            AnsiConsole.MarkupLine($"\n\n[yellow]{prompt}\n{result}[/]");
        }

        await tvClient.DisposeAsync();        

    }

    public class LoggingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Journalisation de la requête
            Console.WriteLine($"Request: {request.Method} {request.RequestUri}");

            if (request.Content != null)
            {
                var requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"========================");
                Console.WriteLine($"Request Content: {requestContent}");
                Console.WriteLine($"========================");
            }

            // Envoi de la requête
            var response = await base.SendAsync(request, cancellationToken);

            Console.WriteLine($"Response: {request.Method} {request.RequestUri}");

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"========================");
            Console.WriteLine($"Response Content: {responseContent}");
            Console.WriteLine($"========================");

            return response;

        }
    }

}