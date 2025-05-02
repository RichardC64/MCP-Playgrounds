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

app.MapGet("/sse", SseHandler.HandleRequestAsync);

app.Run();
return;