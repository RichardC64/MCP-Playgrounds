using System.Text.Json;
using ModelContextProtocol.Protocol;
using Serilog;

namespace Mcp_SQLServer;

public class SqlServerResources(SqlServerResourcesProvider provider)
{

    public async Task<List<Resource>> TablesResources()
    {
        var tables = await provider.GetTableInfosAsync();

        var resources = new List<Resource>();
        
        foreach (var table in tables)
        {
            resources.Add(new Resource
            {
                Name = $"{table.TableName}",
                Description = $"{table.Description}",
                MimeType = "application/json",
                Uri = $"sqlserver://db/tables/{table.TableName}"
            });
            Log.Information($"Table: {table}");
        }

        return resources;
    }

    public async Task<ResourceContents> TableResource(string tableName)
    {
        var columns = await provider.GetColumnInfosAsync(tableName);

        var rs = new TextResourceContents
        {
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(columns),
            Uri = $"sqlserver://db/tables/{tableName}",
        };


        return rs;
    }
}