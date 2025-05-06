using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Protocol.Types;
using Serilog;

public class SqlServerResourcesProvider
{
    public async Task<ListResourcesResult> GetTablesAsync(CancellationToken cancellationToken)
    {
        var resources = new List<Resource>();
        var connectionString = "Server=localhost;Database=TropheeRhune;Trusted_Connection=True;";

        var sql = """
                  USE TropheeRhune; 
                  GO

                  SELECT TABLE_NAME
                  FROM INFORMATION_SCHEMA.TABLES
                  WHERE TABLE_TYPE = 'BASE TABLE'
                  """;

        await using var connection = new SqlConnection(connectionString);

        var r =await connection.QueryAsync<string>(sql);
        foreach (var row in r)
        {
            resources.Add(new Resource
            {
                Name = $"\"{row}\" database schema",
                MimeType = "application/json",
                Uri = $"test://{row}"
            });
            Log.Information($"Table: {row}");
        }

        return new ListResourcesResult
        {
            Resources = resources
        };
    }

    public async Task<ReadResourceResult> GetColumnsAsync(string tableName, CancellationToken cancellationToken)
    {
        var connectionString = "Server=localhost;Database=TropheeRhune;Trusted_Connection=True;";
        var sql = $"""
                  USE TropheeRhune; 
                  GO
                  SELECT COLUMN_NAME as ColumnName, DATA_TYPE s DataType
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = 'dbo'
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
            Contents =[textResourceContents]
        };
    }

    public async Task<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken)
    {

        sql = $"""
               USE TropheeRhune
               GO
               {sql.Trim()}
               """;
        Log.Information($"Execute Query1: {sql}");
        var connectionString = "Server=localhost;Database=TropheeRhune;Trusted_Connection=True;";
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