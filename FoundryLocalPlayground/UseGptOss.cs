using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;

namespace FoundryLocalPlayground;

public class UseGptOss : IUse
{
    private const string _alias = "gpt-oss-20b";
    public async Task ExecuteAsync()
    {
        Console.WriteLine("Démarrage de Foundry Local");
        var manager = await FoundryLocalManager.StartModelAsync(_alias);
        Console.WriteLine("Foundry Local démarré");
        var model = await manager.GetModelInfoAsync(_alias);
        if (model == null) throw new ArgumentException("Model non trouvé");

        var schema = JsonDocument.Parse("""
                                        {
                                          "type":"object",
                                          "properties": { "Final": { "type":"string" } },
                                          "required":["Final"],
                                          "additionalProperties": false
                                        }
                                        """).RootElement;

        var chatClient = new OpenAIClient(
                new ApiKeyCredential(manager.ApiKey),
                new OpenAIClientOptions { Endpoint = manager.Endpoint })
            .GetChatClient(model.ModelId)
            .AsIChatClient();

        List<ChatMessage> chatHistory = new();

        Console.WriteLine("Tapez 'bye' pour terminer");

        var chatOptions = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema(schema),
            StopSequences = ["Thought:", "Réflexion:", "<think>", "</think>"],
            MaxOutputTokens = 2000
        };

        chatHistory.Add(
            new ChatMessage(ChatRole.System,
                "Tu es concis. NE MONTRE JAMAIS tes réflexions internes. Réponds uniquement dans le champ JSON 'final'."));

        while (true)
        {
            Console.Write("Saisissez votre question : ");
            var userInput = Console.ReadLine();

            if (userInput?.ToLower() == "bye")
                break;

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            chatHistory.Add(new ChatMessage(ChatRole.User, userInput));


            // Stream the AI response and display in real time
            Console.Write("Assistant: ");

            var a = await chatClient.GetResponseAsync<FinalOnly>(chatHistory, chatOptions);
            if (a.TryGetResult(out var r))
            {
                Console.WriteLine(r.final);
            }
           

            
            Console.WriteLine();
        }
    }

    public class FinalOnly
    {
        public string final { get; set; }
    }
}