using System.Text.Json;
using Microsoft.Net.Http.Headers;
using SseWebApi;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();

// Redirige la racine vers home.html
app.MapGet("/", async ctx =>
{
    ctx.Response.Redirect("/home.html");
});
app.MapGet("/sse", async (HttpContext ctx, string? action) =>
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
        
        await ctx.Response.WriteAsync($"event: info\ndata: {JsonSerializer.Serialize(response, JsonSerializerOptions.Web)}\n\n");
        await ctx.Response.Body.FlushAsync();

        await Task.Delay(1000);
    }
});

app.Run();