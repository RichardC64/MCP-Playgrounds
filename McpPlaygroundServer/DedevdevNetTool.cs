using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Serilog;

namespace McpPlaygroundServer;

[McpServerToolType]
public static class DedevdevNetTool
{
    [McpServerTool(Name = "devdevdev.NET"), Description("Donne les dernières informations sur les derniers épisode du site devdevdev.net, le podcast des développeurs dotnet .NET")]
    public static async Task<IEnumerable<PostData>> ExecuteGetNews(ILogger logger, HttpClient client, [Description("un mot clé de recherche")] string search)
    {
        logger.Information($"Run {nameof(DedevdevNetTool)}/{nameof(ExecuteGetNews)} Search={search}");
        // historique sur un an seulement
        var response = await client.GetAsync($"/wp-json/wp/v2/posts?per_page=5&search={search}");
        response.EnsureSuccessStatusCode();

        logger.Information($"Response: {response.StatusCode}");

        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        // json is an array of objects, each object has a property called date which is the date, a property called title with a child called rendered and a property called content with a child called rendered.
        var posts = document.RootElement.EnumerateArray()
            .Select(x => new PostData(
                x.GetProperty("date").GetDateTime(),
                x.GetProperty("title").GetProperty("rendered").GetString(),
                x.GetProperty("content").GetProperty("rendered").GetString(),
                x.GetProperty("guid").GetProperty("rendered").GetString()
            )).ToList();

        foreach (var postData in posts)
        {
            logger.Information($"Post: {postData.link} {postData.title}");
        }

        return posts;
    }
    [Description("Représente les données d'un épisode du podcast devdevdev.net")]
    public record PostData(
        [Description("Date de publication de l'épisode")]
        DateTime date,
        [Description("Titre de l'épisode")] string? title,
        [Description("Contenu de l'épisode au format Html")]
        string? content,
        [Description("Adresse Internet de l'épisode. Ne pas la modifier")]
        string? link);
}