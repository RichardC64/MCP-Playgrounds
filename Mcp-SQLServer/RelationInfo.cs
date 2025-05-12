namespace Mcp_SQLServer;

public class RelationInfo
{
    public string ParentTableName { get; set; } = "???";
    public string ParentColumnName { get; set; } = "???";
    public string ReferencedTableName { get; set; } = "???";
    public string ReferencedColumnName { get; set; } = "???";
}