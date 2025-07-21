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
        var searchResults = await vectorStoreService.SearchAsync(queryEmbeddingResult.Vector, limit: 5);

        var context = string.Empty;
        foreach (var result in searchResults)
        {
            if (result.Payload.TryGetValue("text", out var text))
            {
                context += text.ToString();
            }
        }
        var prompt = $"According to the question {question}, optimize and simplify the content. {context}";


        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful assistant that answers questions based on the provided context. Answer in french");
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