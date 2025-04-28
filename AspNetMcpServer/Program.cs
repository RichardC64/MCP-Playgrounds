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


//var mcpServerBuilder = builder.Services.AddMcpServer()

// https://www.strathweb.com/2024/07/built-in-support-for-server-sent-events-in-net-9/
//    .WithHttpTransport(httpTransportOptions =>
//    {
//        httpTransportOptions.RunSessionHandler = (httpContext, mcpServer, cancellationToken) =>
//        {

//            // We could also use ServerCapabilities.NotificationHandlers, but it's good to have some test coverage of RunSessionHandler.
//            mcpServer.RegisterNotificationHandler("test/notification", async (notification, cancellationToken) =>
//            {
//                var message = notification?.Params?["Message"]?.ToString();

//                mcpServer.AsClientLoggerProvider().CreateLogger("test").Log(LogLevel.Information, "coucou");
//                await mcpServer.SendNotificationAsync("test/notification", new Message { Content = "Démarrage du serveur" }, serializerOptions: JsonContext.Default.Options, cancellationToken: cancellationToken);
//            });


//            return mcpServer.RunAsync(cancellationToken);
//        };
//    })
//    .WithTools<DedevdevNetTool>();