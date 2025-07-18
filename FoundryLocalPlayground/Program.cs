using FoundryLocalPlayground;
using Spectre.Console;

var selectedUse = AnsiConsole.Prompt(
    new SelectionPrompt<Uses>()
        .Title("[green]Choisissez le test :[/]")
        .AddChoices(Uses.FoundryOllama, Uses.UseFunction, Uses.UseEmbeddings));

IUse use = selectedUse switch
{
    Uses.FoundryOllama => new UseFoundryOllama(),
    Uses.UseFunction => new UseFunction(),
    Uses.UseEmbeddings => new UseEmbeddings(),
    _ => throw new ArgumentOutOfRangeException(nameof(selectedUse), selectedUse, null)
};

await use.ExecuteAsync();
