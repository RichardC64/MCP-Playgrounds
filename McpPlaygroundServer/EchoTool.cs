using System.ComponentModel;
using ModelContextProtocol.Server;
using Serilog;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool(Name = "Echo"), Description("Répond avec echo le message du client")]
    public static string ExecuteEcho(ILogger logger, string message)
    {
        logger.Information($"Run {nameof(EchoTool)}/{nameof(ExecuteEcho)}");
        return $"Bonjour {message}";
    }
}