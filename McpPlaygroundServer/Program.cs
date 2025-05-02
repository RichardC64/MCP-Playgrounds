using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Types;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Information("Start server...");
builder.Services.AddSerilog(configure =>
{
    configure.MinimumLevel.Verbose();
    configure.WriteTo.File("logs/server_log.txt", rollingInterval: RollingInterval.Day);
});

var _minimumLoggingLevel = LoggingLevel.Debug;

builder.Services
    .AddMcpServer()
    .WithSetLoggingLevelHandler(async (ctx, token) =>
    {
        if (ctx.Params?.Level is null)
        {
            throw new McpException("Missing required argument 'level'", McpErrorCode.InvalidParams);
        }

        _minimumLoggingLevel = ctx.Params.Level;

        await ctx.Server.SendNotificationAsync("notifications/message", new
        {
            Level = "debug",
            Logger = "test-server",
            Data = $"Logging level set to {_minimumLoggingLevel}",
        }, cancellationToken: token);
        return new EmptyResult();
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

builder.Services.AddSingleton<Func<LoggingLevel>>(_ => () => _minimumLoggingLevel);


await builder.Build().RunAsync();



