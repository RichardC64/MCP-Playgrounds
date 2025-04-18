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

builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient { BaseAddress = new Uri("https://devdevdev.net") };
    return client;
});


await builder.Build().RunAsync();



