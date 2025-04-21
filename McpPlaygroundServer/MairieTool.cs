using System.ComponentModel;
using ModelContextProtocol.Server;
using Serilog;

namespace McpPlaygroundServer;

[McpServerToolType]
public static class MairieTool
{
    [McpServerTool(Name = "Adjoints"), Description("Donne la liste des adjoints de TownVille")]
    public static IEnumerable<Adjoint> ExecuteGetAdjoints(ILogger logger)
    {
        logger.Information($"Run {nameof(EchoTool)}/{nameof(ExecuteGetAdjoints)}");
        var adjoints = new List<Adjoint>
        {
            new("Richard", "Adjoint aux sports", 60),
            new("Jean-Jacques", "Adjoints à l'urbanisme", 58)
        };
        return adjoints;
    }
    
    public record Adjoint(string Name, string Function, int Age);
}