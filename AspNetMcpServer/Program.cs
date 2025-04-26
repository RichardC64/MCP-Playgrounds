using AspNetMcpServer;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithSetLoggingLevelHandler(Handler)
    .WithTools<DedevdevNetTool>();

ValueTask<EmptyResult> Handler(RequestContext<SetLevelRequestParams> arg1, CancellationToken arg2)
{
    throw new NotImplementedException();
}

builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient { BaseAddress = new Uri("https://devdevdev.net") };
    return client;
});

var app = builder.Build();

app.MapMcp();

app.Run();


//var mcpServerBuilder = builder.Services.AddMcpServer()
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