using System.ComponentModel;
using ModelContextProtocol.Server;
using Serilog;

namespace Mcp_SQLServer;

[McpServerToolType]
public class QueryTool
{
    [McpServerTool(Name = "query"), Description("Run a read-only SQL query on Microsoft SQL SERVER")]
    public async Task<string> ExecuteQuery(SqlServerResourcesProvider sqlServerResourcesProvider, ILogger logger, string sql)
    {
        logger.Information($"Run {nameof(QueryTool)}/{nameof(ExecuteQuery)} for {sql}");
        return await sqlServerResourcesProvider.ExecuteQueryAsync(sql, CancellationToken.None);
    }


}