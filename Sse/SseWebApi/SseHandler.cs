using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace SseWebApi;

public static class SseHandler
{
    public static async Task HandleRequestAsync(HttpContext ctx, string? action)
    {
        ctx.Response.Headers.Append(HeaderNames.ContentType, "text/event-stream");
        while (!ctx.RequestAborted.IsCancellationRequested)
        {
            decimal value;
            switch (action)
            {
                case "MSFT":
                    value = Random.Shared.Next(200, 400);
                    break;
                case "AAPL":
                    value = Random.Shared.Next(0, 100);
                    break;
                case "TSLA":
                    value = Random.Shared.Next(-100, 0);
                    break;
                default:
                    value = 0;
                    action = "???";
                    break;
            }

            var response = new SseResponse(DateTime.Now, action, value);

            // la réponse doit être au format : event: <event-name>\ndata: <data>\n\n ou data: <data>\n\n
            await ctx.Response.WriteAsync($"event: info\ndata: {JsonSerializer.Serialize(response, JsonSerializerOptions.Web)}\n\n");
            await ctx.Response.Body.FlushAsync();

            // mise à jour toutes les secondes
            await Task.Delay(1000);
        }
    }
}