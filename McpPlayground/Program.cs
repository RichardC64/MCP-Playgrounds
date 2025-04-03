using McpPlayground;
using Spectre.Console;

var tools = new[] { nameof(UseMcpPlaygroundServer), nameof(UsePlaywrightServer), nameof(UseGithubServer) };
var tool = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[green]Choisissez votre outil :[/]")
        .AddChoices(tools));

// Echo the fruit back to the terminal
AnsiConsole.MarkupLine($"[green]Invocation de {tool}...[/]");

switch (tool)
{
    case nameof(UseMcpPlaygroundServer):
        await UseMcpPlaygroundServer.ExecuteAsync();
        break;
    case nameof(UsePlaywrightServer):
        await UsePlaywrightServer.ExecuteAsync();
        break;
    case nameof(UseGithubServer):
        await UseGithubServer.ExecuteAsync();
        break;
    default:
        AnsiConsole.MarkupLine($"[red]Outil {tool} non pris en charge.[/]");
        break;
}

Console.ReadLine();