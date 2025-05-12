using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using ModelContextProtocol;
using Serilog;

namespace Mcp_SQLServer;

public class SqlServerResourcesProvider(string connectionString)
{

    public async Task<IEnumerable<TableInfo>> GetTableInfosAsync()
    {
        var sql = """
                  SELECT 
                      t.name AS TableName,
                      ep.value AS Description
                  FROM 
                      sys.tables t
                  INNER JOIN 
                      sys.schemas s ON t.schema_id = s.schema_id
                  LEFT JOIN 
                      sys.extended_properties ep 
                      ON ep.major_id = t.object_id 
                      AND ep.minor_id = 0 
                      AND ep.name = 'MS_Description'
                  ORDER BY 
                      t.name;
                  """;

        await using var connection = new SqlConnection(connectionString);
        return await connection.QueryAsync<TableInfo>(sql);
    }
   

    public async Task<IEnumerable<ColumnInfo>> GetColumnInfosAsync(string tableName)
    {
        var sql = """
                   SELECT 
                       c.name as ColumnName,
                       ty.name AS DataType,
                       c.max_length AS MaxLength,
                       c.is_nullable AS IsNullable,
                       c.is_identity as IsIdentity,
                       ep.value as Description
                   FROM 
                       sys.columns c
                   INNER JOIN 
                       sys.tables t ON c.object_id = t.object_id
                   	INNER JOIN 
                       sys.types ty ON c.user_type_id = ty.user_type_id
                   LEFT JOIN 
                       sys.extended_properties ep 
                       ON ep.major_id = c.object_id 
                       AND ep.minor_id = c.column_id 
                       AND ep.name = 'MS_Description'
                   WHERE 
                       t.name = @tableName;
                   """;
        await using var connection = new SqlConnection(connectionString);
        try
        {
            return await connection.QueryAsync<ColumnInfo>(sql, new {tableName});
        }
        catch (Exception e)
        {
            Log.Error(e, "Erreur : {e}",e);
            throw new McpException("pas cool");
        }
        
    }

    public async Task<IEnumerable<RelationInfo>> GetRelationInfosAsync()
    {
        var sql = """
                  SELECT 
                      sch_parent.name + '.' + t_parent.name AS ParentTableName,
                      col_parent.name AS ParentColumnName,
                      sch_ref.name + '.' + t_ref.name AS ReferencedTableName,
                      col_ref.name AS ReferencedColumnName
                  FROM 
                      sys.foreign_keys fk
                  INNER JOIN 
                      sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                  INNER JOIN 
                      sys.tables t_parent ON fkc.parent_object_id = t_parent.object_id
                  INNER JOIN 
                      sys.columns col_parent ON fkc.parent_object_id = col_parent.object_id AND fkc.parent_column_id = col_parent.column_id
                  INNER JOIN 
                      sys.tables t_ref ON fkc.referenced_object_id = t_ref.object_id
                  INNER JOIN 
                      sys.columns col_ref ON fkc.referenced_object_id = col_ref.object_id AND fkc.referenced_column_id = col_ref.column_id
                  INNER JOIN 
                      sys.schemas sch_parent ON t_parent.schema_id = sch_parent.schema_id
                  INNER JOIN 
                      sys.schemas sch_ref ON t_ref.schema_id = sch_ref.schema_id
                  """;
        await using var connection = new SqlConnection(connectionString);
        try
        {
            return await connection.QueryAsync<RelationInfo>(sql);
        }
        catch (Exception e)
        {
            Log.Error(e, "Erreur : {e}", e);
            throw new McpException("pas cool");
        }

    }


    public async Task<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken)
    {

        sql = $"""
               {sql.Trim()}
               """;
        Log.Information($"Execute Query1: {sql}");
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