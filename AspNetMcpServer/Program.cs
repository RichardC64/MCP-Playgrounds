using AspNetMcpServer;
using ModelContextProtocol;
using System.Net.Http.Json;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport(httpTransportOptions =>
    {
        httpTransportOptions.RunSessionHandler = (httpContext, mcpServer, cancellationToken) =>
        {
            // We could also use ServerCapabilities.NotificationHandlers, but it's good to have some test coverage of RunSessionHandler.
            mcpServer.RegisterNotificationHandler("test/notification", async (notification, cancellationToken) =>
            {
                var message =  notification?.Params?["Message"]?.ToString();

                mcpServer.AsClientLoggerProvider().CreateLogger("test").Log(LogLevel.Critical, "coucou");
                Console.WriteLine($"Received notification: {message}");
                await mcpServer.SendNotificationAsync("test/notification", new Message{Content = "Hello from server!" }, cancellationToken: cancellationToken);
            });
            return mcpServer.RunAsync(cancellationToken);
        };


    })
    .WithTools<DedevdevNetTool>();

builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient { BaseAddress = new Uri("https://devdevdev.net") };
    return client;
});

var app = builder.Build();

app.MapMcp();

app.Run();