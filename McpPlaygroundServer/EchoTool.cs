using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool(Name="Echo"), Description("Répond avec echo le message du client")]
    public static string ExecuteEcho(string message) => $"Bonjour {message}";
}