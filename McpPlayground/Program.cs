using McpPlayground;
using Serilog;
using Spectre.Console;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("logs/client_log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("Start Client");

var tools = new[] { nameof(UseMcpSQLServer), nameof(UseMcpPlaygroundServer), nameof(UseMcpPlaygroundServerComplexDatas), nameof(UseAiTownVilleServer), nameof(UseAiWithSseServer), nameof(UsePlaywrightServer) };
var tool = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[green]Choisissez votre outil :[/]")
        .AddChoices(tools));

// Echo the fruit back to the terminal
AnsiConsole.MarkupLine($"[green]Invocation de {tool}...[/]");

switch (tool)
{
    case nameof(UseMcpSQLServer):
        await UseMcpSQLServer.ExecuteAsync();
        break;
    case nameof(UseMcpPlaygroundServer):
        await UseMcpPlaygroundServer.ExecuteAsync();
        break;
    case nameof(UseMcpPlaygroundServerComplexDatas):
        await UseMcpPlaygroundServerComplexDatas.ExecuteAsync();
        break;
    case nameof(UsePlaywrightServer):
        await UsePlaywrightServer.ExecuteAsync();
        break;
    case nameof(UseAiTownVilleServer):
        await UseAiTownVilleServer.ExecuteAsync();
        break;
    case nameof(UseAiWithSseServer):
        await UseAiWithSseServer.ExecuteAsync();
        break;
    default:
        AnsiConsole.MarkupLine($"[red]Outil {tool} non pris en charge.[/]");
        break;
}

AnsiConsole.MarkupLine("[green]Appuyez sur une touche pour quitter...[/]");
Console.ReadLine();