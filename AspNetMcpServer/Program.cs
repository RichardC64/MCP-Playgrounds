using AspNetMcpServer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<DedevdevNetTool>();

builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient { BaseAddress = new Uri("https://devdevdev.net") };
    return client;
});

var app = builder.Build();

app.MapMcp();

app.Run();