using System.Text;
using System.Text.Json;
using LoreForge.Core.Ports;
using Microsoft.Extensions.Logging;

namespace LoreForge.Infrastructure.Bedrock.AgentTools;

public class SearchInspirationTool(
    IEmbeddingService embedding,
    IVectorStore vectorStore,
    ILogger<SearchInspirationTool> logger) : IAgentTool
{
    private const int TopK = 5;

    public string Name => "search_inspiration";

    public string Description =>
        "Searches the user's logbook of games, books, movies, and shows, plus their journal entries, " +
        "to find relevant inspiration for a creative writing question.";

    public JsonElement InputSchema => JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "The semantic search query describing what to look for."
                }
            },
            "required": ["query"]
        }
        """).RootElement;

    public async Task<string> ExecuteAsync(JsonElement input, CancellationToken ct)
    {
        var query = input.GetProperty("query").GetString()!;
        logger.LogInformation("search_inspiration invoked with query: {Query}", query);

        var queryEmbedding = await embedding.EmbedAsync(query, ct);

        var works = await vectorStore.SearchWorksAsync(queryEmbedding, TopK, ct);
        var entries = await vectorStore.SearchJournalEntriesAsync(queryEmbedding, TopK, ct);

        logger.LogInformation("search_inspiration found {Works} works and {Entries} journal entries", works.Count, entries.Count);

        var sb = new StringBuilder();

        if (works.Count > 0)
        {
            sb.AppendLine("## Relevant works from logbook");
            foreach (var work in works)
            {
                sb.AppendLine($"### {work.Title} ({work.Type}, {work.Status})");
                if (work.Genres.Length > 0)
                    sb.AppendLine($"Genres: {string.Join(", ", work.Genres)}");
                sb.AppendLine(work.Notes.ToEmbeddingText(work.Title));
                sb.AppendLine();
            }
        }

        if (entries.Count > 0)
        {
            sb.AppendLine("## Relevant journal entries");
            foreach (var entry in entries)
            {
                sb.AppendLine($"- [{entry.CreatedAt:yyyy-MM-dd}] {entry.RawContent}");
            }
        }

        if (works.Count == 0 && entries.Count == 0)
            return "No relevant inspiration found in the logbook.";

        return sb.ToString();
    }
}
