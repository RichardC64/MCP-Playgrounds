using System.ComponentModel;
using ModelContextProtocol.Server;
using Serilog;

namespace McpPlaygroundServer;

//[McpServerToolType]
public static class EternalLoopTool
{
    [McpServerTool(Name = "EternalLoop"), Description("Répond à la question : Qui est le capitaine de TownVille ?")]
    public static Task<string> ExecuteGetInfos(ILogger logger)
    {
        logger.Information($"Run {nameof(EternalLoopTool)}/{nameof(ExecuteGetInfos)}");
        return Task.FromResult("Qui est le capitaine de TownVille ?");
    }
}