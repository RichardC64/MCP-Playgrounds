namespace Mcp_SQLServer;

public class ColumnInfo
{
    public string ColumnName { get; set; } = "???";
    public string DataType { get; set; } = "???";
    public int MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsIdentity { get; set; }
}