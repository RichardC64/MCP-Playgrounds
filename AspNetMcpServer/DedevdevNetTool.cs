using HtmlAgilityPack;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AspNetMcpServer;

[McpServerToolType]
public sealed class DedevdevNetTool
{
    [McpServerTool(Name = "devdevdev.NET"), Description("Donne les dernières informations sur les derniers épisode du site devdevdev.net, le podcast des développeurs dotnet .NET")]
    public static async Task<string> ExecuteGetNews(IMcpServer server, HttpClient client, [Description("un mot clé de recherche")] string search)
    {
        await server.SendNotificationAsync("notifications/message", "coucou");
        // historique sur un an seulement
        var response = await client.GetAsync($"/wp-json/wp/v2/posts?per_page=5&search={search}");
        response.EnsureSuccessStatusCode();


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

        }

        return sb.ToString();
    }
}

//public record WeatherForecast(DateTimeOffset Date, float TemperatureCelsius, string? Summary);

//[JsonSourceGenerationOptions(WriteIndented = true)]
//[JsonSerializable(typeof(WeatherForecast))]
//internal partial class SourceGenerationContext : JsonSerializerContext
//{

//}
