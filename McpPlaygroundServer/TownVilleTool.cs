using System.ComponentModel;
using ModelContextProtocol.Server;
using Spectre.Console;

namespace McpPlaygroundServer;

[McpServerToolType]
public static class TownVilleTool
{
    [McpServerTool(Name = "TownVilleNews"), Description("Donne les dernières informations concernant la ville de TownVille")]
    public static string ExecuteGetNews()
    {
        AnsiConsole.MarkupLine($"[red]Run {nameof(TownVilleTool)}/{nameof(ExecuteGetNews)}[/]");
        return $"""
                  Aujourd'hui {DateTime.Today.Date:dd/MM/yyyy} :
                  Il fera beau avec une température de 25°C.
                  Notre maire, Monsieur Jean Bonneau, inaugure le nouveau parc de la ville.
                  L'équipe de Rugby TRC a gagné perdu 55 à 10 contre l'Aviron Bayonnais.
                  """;
    }
}