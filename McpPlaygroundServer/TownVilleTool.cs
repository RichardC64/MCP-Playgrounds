using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class TownVilleTool
{
    [McpServerTool(Name = "TownVilleNews"), Description("Donne les dernières informations concernant la ville de TownVille")]
    public static string ExecuteGetNews()
    {
        Console.WriteLine("Executing TownVilleTool");
        return $"Il fait toujours beau à TownVille surtout en ce {DateTime.Today.Date:dd/MM/yyyy}";
    }
}