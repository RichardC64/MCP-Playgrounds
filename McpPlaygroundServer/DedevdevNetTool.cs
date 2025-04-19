using System.ComponentModel;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using ModelContextProtocol.Server;
using Serilog;

namespace McpPlaygroundServer;

[McpServerToolType]
public static class DedevdevNetTool
{
    [McpServerTool(Name = "devdevdev.NET"), Description("Donne les dernières informations sur les derniers épisode du site devdevdev.net, le podcast des développeurs dotnet .NET")]
    public static async Task<string> ExecuteGetNews(ILogger logger, HttpClient client, [Description("un mot clé de recherche")] string search)
    {
        logger.Information($"Run {nameof(DedevdevNetTool)}/{nameof(ExecuteGetNews)} Search={search}");
        // historique sur un an seulement
        var response = await client.GetAsync($"/wp-json/wp/v2/posts?per_page=5&search={search}");
        response.EnsureSuccessStatusCode();

        logger.Information($"Response: {response.StatusCode}");

        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
         var sb = new StringBuilder();
        foreach (var post in document.RootElement.EnumerateArray())
        {
            var doc = new HtmlDocument();
            var content = post.GetProperty("content").GetProperty("rendered").GetString();

            if (content != null)
            {
                doc.LoadHtml(content);
                content = doc.DocumentNode.InnerText;
            }

            sb.AppendLine($"Date: {post.GetProperty("date").GetDateTime()}");
            sb.AppendLine($"Titre: {post.GetProperty("title").GetProperty("rendered").GetString()}");
            sb.AppendLine($"Lien: {post.GetProperty("guid").GetProperty("rendered").GetString()}");
            sb.AppendLine($"Contenu: {content}");
            sb.AppendLine("");
            sb.AppendLine("");

            logger.Debug(post.GetProperty("guid").GetProperty("rendered").GetString() ?? "???");
        }

        return sb.ToString();
    }
}