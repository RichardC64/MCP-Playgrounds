using System.Text.Json;
using System.Text.Json.Serialization;
using Mcp_SQLServer;
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
    .WithListResourcesHandler(async (ctx, ct) =>
    {
        return await new SqlServerResourcesProvider().GetTablesAsync(ct);
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
        return await new SqlServerResourcesProvider().GetColumnsAsync(tableName, ct);
    })
    .WithToolsFromAssembly();

builder.Services.AddSingleton<Func<LoggingLevel>>(_ => () => _minimumLoggingLevel);

//resources: result.rows.map((row) => ({
//    uri: new URL(`${ row.table_name } /${ SCHEMA_PATH}`, resourceBaseUrl).href,
//    mimeType: "application/json",
//    name: `"${row.table_name}" database schema`,
//})),


await builder.Build().RunAsync();