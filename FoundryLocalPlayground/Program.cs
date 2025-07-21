using FoundryLocalPlayground;
using Spectre.Console;

var selectedUse = AnsiConsole.Prompt(
    new SelectionPrompt<Uses>()
        .Title("[green]Choisissez le test :[/]")
        .AddChoices(Uses.UseEmbeddingsFl, Uses.FoundryOllama, Uses.UseFunction, Uses.UseEmbeddingsSk));

IUse use = selectedUse switch
{
    Uses.FoundryOllama => new UseFoundryOllama(),
    Uses.UseFunction => new UseFunction(),
    Uses.UseEmbeddingsSk => new UseEmbeddingsSk(),
    Uses.UseEmbeddingsFl => new UseEmbeddingsMs(),

    _ => throw new ArgumentOutOfRangeException(nameof(selectedUse), selectedUse, null)
};

await use.ExecuteAsync();
