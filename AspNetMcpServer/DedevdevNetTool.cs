using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace AspNetMcpServer;

[McpServerToolType]
public sealed class DedevdevNetTool
{
    [McpServerTool(Name = "devdevdev.NET"), Description("Donne les dernières informations sur les derniers épisode du site devdevdev.net, le podcast des développeurs dotnet .NET")]
    public static async Task<string> ExecuteGetNews(IMcpServer server, HttpClient client, [Description("un mot clé de recherche")] string search)
    {
        await server.SendNotificationAsync("notifications/message", "Appel API");
        // historique sur un an seulement
        var response = await client.GetAsync($"/wp-json/wp/v2/posts?per_page=5&search={search}");
        response.EnsureSuccessStatusCode();

        await server.SendNotificationAsync("notifications/message", "Appel API terminé");
        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
         var sb = new StringBuilder();
        foreach (var post in document.RootElement.EnumerateArray())
        {
            sb.AppendLine($"Date: {post.GetProperty("date").GetDateTime()}");
            sb.AppendLine($"Titre: {post.GetProperty("title").GetProperty("rendered").GetString()}");
            sb.AppendLine($"Lien: {post.GetProperty("guid").GetProperty("rendered").GetString()}");
            sb.AppendLine("");
        }
        return sb.ToString();
    }
}
