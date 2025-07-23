using Microsoft.Extensions.AI;
using Spectre.Console;

namespace FoundryLocalPlayground;

public class DocumentIngestionService(IEmbeddingGenerator<string, Embedding<float>> embeddingService, VectorStoreService vectorStoreService)
{
    public async Task IngestDocumentAsync(string documentPath, string documentId)
    {
        var content = await File.ReadAllTextAsync(documentPath);
        var chunks = ChunkText(content, 300, 60);

        var increment = 100 / chunks.Count;
        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                // Define tasks
                var task = ctx.AddTask("[yellow]Chargement du document[/]");

                
                for (var i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    var embeddingResult = await embeddingService.GenerateAsync(chunk, new EmbeddingGenerationOptions
                    {
                        Dimensions = 119547
                    });

                    
                    await vectorStoreService.UpsertAsync(
                        embedding: embeddingResult.Vector,
                        metadata: new Dictionary<string, object>
                        {
                            ["document_id"] = documentId,
                            ["chunk_index"] = i,
                            ["text"] = chunk,
                            ["document_path"] = documentPath
                        }
                    );
                    task.Increment(increment);
                }
            });      
       
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < words.Length; i += chunkSize - overlap)
        {
            var chunkWords = words.Skip(i).Take(chunkSize).ToArray();
            var chunk = string.Join(" ", chunkWords);
            chunks.Add(chunk);

            if (i + chunkSize >= words.Length)
                break;
        }

        return chunks;
    }
}