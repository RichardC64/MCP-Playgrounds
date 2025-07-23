using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FoundryLocalPlayground;

internal class RagQueryService(
    IEmbeddingGenerator<string, Embedding<float>> embeddingService,
    IChatCompletionService chatService,
    VectorStoreService vectorStoreService)
{
    public async Task<string> QueryAsync(string question)
    {
        var queryEmbeddingResult = await embeddingService.GenerateAsync(question);
        var searchResults = await vectorStoreService.SearchAsync(queryEmbeddingResult.Vector, limit: 3);

        var context = string.Empty;
        foreach (var result in searchResults)
        {
            if (result.Payload.TryGetValue("text", out var text))
            {
                context += text.ToString();
            }
        }
        var prompt = $"Répond à la question : {question}, optimise et simplifie le contexte : {context}";


        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("Tu es un assistant qui répond aux question basé sur le contexte fourni. Répond le plus simplement possible en une seule phrase en français.");
        chatHistory.AddUserMessage(prompt);

        var fullMessage = string.Empty;

        await foreach (var chatUpdate in chatService.GetStreamingChatMessageContentsAsync(chatHistory))
        {
            if (chatUpdate.Content is { Length: > 0 })
            {
                fullMessage += chatUpdate.Content;
            }
        }
        return fullMessage;
    }
}