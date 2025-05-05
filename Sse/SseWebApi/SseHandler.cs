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

            var response = new MySseResponse(DateTime.Now, action, value);

            /*
            // la réponse doit être au format : event: <event-name>\ndata: <data>\n\n ou data: <data>\n\n
            // exemple :
            // event: info\ndata: {"date":"2025-05-02T09:55:23.2961256+02:00","action":"MSFT","value":310}\n\n
            // avec MCP, le format est du JsonRPC soit un appel du client au format :
            // {
            //    "jsonrpc": "2.0",
            //    "method": "nomDeLaMéthode",
            //    "params": ["param1", "param2"],
            //    "id": 4
            // }

            // et la réponse du serveur au format :
            // {
            //    "jsonrpc": "2.0",
            //    "result": {
            //        "date": "2025-05-02T09:55:23.2961256+02:00",
            //        "action": "MSFT",
            //        "value": 310
            // },
            //    "id": 4
            */

            await ctx.Response.WriteAsync($"event: info\ndata: {JsonSerializer.Serialize(response, JsonSerializerOptions.Web)}\n\n");
            await ctx.Response.Body.FlushAsync();

            // mise à jour toutes les secondes
            await Task.Delay(1000);
        }
    }
}