using System.ComponentModel;
using ModelContextProtocol.Server;
using Spectre.Console;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool(Name="Echo"), Description("Répond avec echo le message du client")]
    public static string ExecuteEcho(string message)
    {
        AnsiConsole.MarkupLine($"[red]Run {nameof(EchoTool)}/{nameof(ExecuteEcho)}[/]");
        return $"Bonjour {message}";
    }
}