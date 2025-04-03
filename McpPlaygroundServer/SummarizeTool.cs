using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class SummarizeTool
{
    [McpServerTool(Name = "ResumeDepuisUrl"), Description("Résume le contenu à partir d'une url")]
    public static async Task<string> SummarizeDownloadedContent(
        IMcpServer thisServer,
        HttpClient httpClient,
        [Description("Url avec le contenu à télécharger")]
        string url,
        CancellationToken cancellationToken)
    {
        var content = await httpClient.GetStringAsync(url, cancellationToken);

        ChatMessage[] messages =
        [
            new(ChatRole.User, "Résume le contenu téléchargé suivant :"),
            new(ChatRole.User, content),
        ];

        ChatOptions options = new()
        {
            MaxOutputTokens = 256,
            Temperature = 0.3f,
        };

        return
            $"Résumé: {await thisServer.AsSamplingChatClient().GetResponseAsync(messages, options, cancellationToken)}";
    }
}