using System.ComponentModel;
using ModelContextProtocol.Server;
using Serilog;

namespace Mcp_SQLServer;

[McpServerToolType]
public class QueryTool
{
    [McpServerTool(Name = "query"), Description("Run a read-only SQL query")]
    public async Task<string> ExecuteQuery(ILogger logger, string sql)
    {
        logger.Information($"Run {nameof(QueryTool)}/{nameof(ExecuteQuery)} for {sql}");
        var q = new SqlServerResourcesProvider();
        return await q.ExecuteQueryAsync(sql, CancellationToken.None);
    }


}