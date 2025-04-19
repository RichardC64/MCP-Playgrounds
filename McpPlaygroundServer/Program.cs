using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Information("Start server...");
builder.Services.AddSerilog(configure =>
{
    configure.MinimumLevel.Verbose();
    configure.WriteTo.File("logs/server_log.txt", rollingInterval: RollingInterval.Day);
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();


await builder.Build().RunAsync();



