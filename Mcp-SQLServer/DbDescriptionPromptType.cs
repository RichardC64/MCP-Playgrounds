using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Mcp_SQLServer;

[McpServerPromptType]
public class DbDescriptionPromptType
{
    [McpServerPrompt(Name = "Description de la base de données"), Description("Un prompt qui décrit la base de données")]
    public async Task<IEnumerable<ChatMessage>> DbDescriptionPrompt(SqlServerResourcesProvider provider)
    {
        var messages = new List<string>();
        messages.Add("Voici la description des tables de la base de données");

        messages.Add("Liste des tables :");
        var tables = await provider.GetTableInfosAsync();
        foreach (var tableInfo in tables)
        {
            messages.Add($"Table {tableInfo.TableName} : {tableInfo.Description}");

            var columns = await provider.GetColumnInfosAsync(tableInfo.TableName);
            messages.Add($"Liste des colonnes de la table {tableInfo.TableName} :");
            foreach (var columnInfo in columns)
            {
                messages.Add($"Colonne {columnInfo.ColumnName} : {columnInfo.Description}, Type de données : {columnInfo.DataType}, Taille : {columnInfo.MaxLength}, Nullable : {columnInfo.IsNullable}, Identité : {columnInfo.IsIdentity}");
            }

        }

        messages.Add("Fin de la liste des tables");
        messages.Add("Liste des relations entre les tables :");

        messages.Add("Voici la liste des relations entre les tables :");
        var relations = await provider.GetRelationInfosAsync();
        foreach (var relationInfo in relations)
        {
            messages.Add($"La table `{relationInfo.ParentTableName}` contient une colonne `{relationInfo.ParentColumnName}` qui est une clé étrangère référant la colonne `{relationInfo.ReferencedColumnName}` de la table `{relationInfo.ReferencedTableName}`");
        }
        messages.Add("Fin de la liste des relations entre les tables");


        return messages.Select(m => new ChatMessage(ChatRole.Assistant, m));
    }
}