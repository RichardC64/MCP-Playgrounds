using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Serilog;
using Spectre.Console;
using System.Text.Json;
using Mcp_SQLServer;
using ModelContextProtocol.Protocol.Types;

namespace McpPlayground;

public static class UseMcpSQLServer
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

        using var client = new OllamaChatClient("http://localhost:11434/", "llama3.1", httpClient)
            .AsBuilder()
            .UseFunctionInvocation()
            .UseLogging(loggerFactory)
            .Build();

        var transportOptions = new StdioClientTransportOptions
        {
            Name = "myserver",
            Command = "dotnet",
            Arguments = ["run", "--project", @"..\..\..\..\Mcp-SQLServer", "--", "Server=(local);Database=TropheeRhune;Trusted_Connection=True;TrustServerCertificate=true"]
        };

        // en mode release, ajouter l'argument --no-build

        await using var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(transportOptions), null, loggerFactory);
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

        // Resources
        var ressources = await mcpClient.ListResourcesAsync();

        foreach (var ressource in ressources)
        {
            AnsiConsole.MarkupLine($"[Blue]{ressource.Name}[/]");
            AnsiConsole.WriteLine($"{ressource.Description}");
            var res = await mcpClient.ReadResourceAsync(ressource.Uri).ConfigureAwait(false);
            var re = res.Contents.FirstOrDefault() as TextResourceContents;

            if (re?.Text != null)
            {
                var table = new Table();

                table.AddColumn("Nom");
                table.AddColumn("Type");
                table.AddColumn("Description");

                var columnInfos =  JsonSerializer.Deserialize<IEnumerable<ColumnInfo>>(re.Text);
                if (columnInfos == null) continue;
                foreach (var columnInfo in columnInfos)
                {
                    table.AddRow(columnInfo.ColumnName, columnInfo.DataType, columnInfo.Description);
                }

                AnsiConsole.Write(table);
            }
        }
        // prompts
        var prompts = await mcpClient.ListPromptsAsync().ConfigureAwait(false);

        var messages = new List<ChatMessage>();
        foreach (var prompt in prompts)
        {
            AnsiConsole.MarkupLine($"[Blue]{prompt.Name}[/]");
            AnsiConsole.WriteLine($"{prompt.Description}");
            AnsiConsole.WriteLine($"{prompt.ProtocolPrompt.Name}");

            var pt = await mcpClient.GetPromptAsync(prompt.Name);
            foreach (var promptMessage in pt.Messages)
            {
                Console.WriteLine($"{promptMessage.Role} : {promptMessage.Content.Text}");
                messages.Add(new ChatMessage( promptMessage.Role == Role.Assistant ? ChatRole.Assistant : ChatRole.User, promptMessage.Content.Text));
            }
        }

        while (true)
        {
            var prompt = AnsiConsole.Prompt(
                new TextPrompt<string>("Votre question ? ('bye' pour terminer)"));

            if (string.Equals("bye", prompt, StringComparison.InvariantCultureIgnoreCase))
                break;

            var cts = new CancellationTokenSource();
            // add messages to the prompt
            messages.Add(new ChatMessage(ChatRole.User, prompt));

            var task = client.GetStreamingResponseAsync(messages, new()
            {
                Tools = [.. tools],
                
            }, cts.Token);
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
}