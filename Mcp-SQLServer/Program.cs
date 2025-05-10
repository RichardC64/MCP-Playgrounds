using Mcp_SQLServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Types;
using Serilog;


var builder = Host.CreateApplicationBuilder(args);

// try to retrieve first argument from args
var connectionString = args.Length > 0 ? args[0] : null;
if (connectionString is null)
{
    Console.WriteLine("Missing required argument 'connectionString'");
    return 1;
}
builder.Services.AddSerilog(configure =>
{
    configure.MinimumLevel.Verbose();
    configure.WriteTo.File("logs/server_log.txt", rollingInterval: RollingInterval.Day);
});
builder.Services.AddSingleton<SqlServerResourcesProvider>(_ => new SqlServerResourcesProvider(connectionString));

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
    .WithListResourcesHandler(async (ctx, ct) =>
    {
        var sqlServerResourcesProvider = ctx.Server.Services?.GetService<SqlServerResourcesProvider>();
        if (sqlServerResourcesProvider is null)
            throw new McpException("SqlServerResourcesProvider not found", McpErrorCode.InternalError);
        return await sqlServerResourcesProvider.GetTablesAsync(ct);
    })
    .WithReadResourceHandler(async (ctx, ct) =>
    {
        var uri = ctx.Params?.Uri;
        // convert to uri
        if (uri is null)
        {
            throw new McpException("Missing required argument 'uri'", McpErrorCode.InvalidParams);
        }
        // check if uri is valid
        if (!uri.StartsWith("test://"))
        {
            throw new McpException($"Invalid uri: {uri}", McpErrorCode.InvalidParams);
        }
        var tableName = uri.Substring("test://".Length);

        var sqlServerResourcesProvider = ctx.Server.Services?.GetService<SqlServerResourcesProvider>();
        if (sqlServerResourcesProvider is null)
            throw new McpException("SqlServerResourcesProvider not found", McpErrorCode.InternalError);

        return await sqlServerResourcesProvider.GetColumnsAsync(tableName, ct);
    })
    .WithToolsFromAssembly();


builder.Services.AddSingleton<Func<LoggingLevel>>(_ => () => _minimumLoggingLevel);

var host = builder.Build();

Log.Information("Start server connected to {connectionString}...", connectionString);
await host.RunAsync();

return 0;