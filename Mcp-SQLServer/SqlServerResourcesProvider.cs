using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Protocol.Types;
using Serilog;

namespace Mcp_SQLServer;

public class SqlServerResourcesProvider
{
    public async Task<ListResourcesResult> GetTablesAsync(CancellationToken cancellationToken)
    {
        var resources = new List<Resource>();
        var connectionString = "Server=(local);Database=TropheeRhune;Trusted_Connection=True;TrustServerCertificate=true";

        var sql = """
                  SELECT TABLE_NAME
                  FROM INFORMATION_SCHEMA.TABLES
                  WHERE TABLE_TYPE = 'BASE TABLE';
                  """;

        await using var connection = new SqlConnection(connectionString);

        var tables = await connection.QueryAsync<string>(sql);
        foreach (var table in tables)
        {
            resources.Add(new Resource
            {
                Name = $"\"{table}\" database schema",
                Description = $"Table \"{table}\"",
                MimeType = "application/json",
                Uri = $"test://{table}"
            });
            Log.Information($"Table: {table}");
        }

        return new ListResourcesResult
        {
            Resources = resources
        };
    }

    public async Task<ReadResourceResult> GetColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        var connectionString = "Server=(local);Database=TropheeRhune;Trusted_Connection=True;TrustServerCertificate=true";
        var sql = $"""
                   SELECT COLUMN_NAME as ColumnName, DATA_TYPE as DataType
                   FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = 'dbo';
                   """;
        await using var connection = new SqlConnection(connectionString);
        var columns = await connection.QueryAsync<ColumnInfo>(sql);
        var textResourceContents = new TextResourceContents
        {
            Text = JsonSerializer.Serialize(columns),
            MimeType = "application/json",
            Uri = $"test://{tableName}"
        };
        Log.Information($"{textResourceContents.Text}");
        return new ReadResourceResult
        {
            Contents = [textResourceContents]
        };
    }

    public async Task<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken)
    {

        sql = $"""
               {sql.Trim()}
               """;
        Log.Information($"Execute Query1: {sql}");
        var connectionString = "Server=(local);Database=TropheeRhune;Trusted_Connection=True;TrustServerCertificate=true";
        await using var connection = new SqlConnection(connectionString);
        try
        {
            var result = (await connection.QueryAsync(sql)).ToList();
            Log.Information($"Execute Query: {result.Count}");
            return JsonSerializer.Serialize(result);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            throw;
        }

    }
}

public class ColumnInfo
{
    public string ColumnName { get; set; }
    public string DataType { get; set; }
}