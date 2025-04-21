using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;


await TestClass.ExecuteAsync();
Console.ReadLine();

public static class TestClass
{
    public static async Task ExecuteAsync()
    {
        // Prepare and build kernel

        
        using IChatClient ollamaClient = (new OllamaChatClient("http://localhost:11434/", "llama3"));

        using var client = new ChatClientBuilder(ollamaClient)
            .UseFunctionInvocation()
            .Build();

        var loggerFactory = LoggerFactory.Create(configure => configure.AddConsole());


        // Create an MCPClient for the GitHub server
        await using var mcpClient = await McpClientFactory.CreateAsync(
            new McpServerConfig
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
            }, null, loggerFactory).ConfigureAwait(false);

        var githubTools = await mcpClient.ListToolsAsync().ConfigureAwait(false);

        var tvClient = await McpClientFactory.CreateAsync(new()
        {
            Id = "everything",
            Name = "Everything",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new()
            {
                ["command"] = @"..\..\..\..\McpPlaygroundServer\bin\Debug\net9.0\McpPlaygroundServer.exe"
            }
        });

        var tvTools = await tvClient.ListToolsAsync().ConfigureAwait(false);

        var allTools = githubTools.Concat(tvTools).ToList();

        //var prompt = "give me a summary of the last 4 commits on the microsoft/semantic-kernel repository?";
        var prompt = "quelles sont les dernières informations concernant la ville de TownVille?";
        var result = await client.GetResponseAsync(prompt, new()
        {
            Tools = [..allTools],
            Temperature = 0
        });

        Console.WriteLine($"\n\n{prompt}\n{result}");

    }

}