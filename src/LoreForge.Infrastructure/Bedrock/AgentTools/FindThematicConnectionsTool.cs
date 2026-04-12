using System.Text;
using System.Text.Json;
using LoreForge.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LoreForge.Infrastructure.Bedrock.AgentTools;

public class FindThematicConnectionsTool(
    IEmbeddingService embedding,
    IVectorStore vectorStore,
    IConfiguration config,
    ILogger<FindThematicConnectionsTool> logger) : IAgentTool
{
    private const int TopK = 5;

    public string Name => "find_thematic_connections";

    public string Description =>
        "Searches the user's logbook and journal entries for recurring themes, motifs, tones, moral questions, " +
        "and symbolic elements the user gravitates toward, so the AI can help weave those threads into their own story.";

    public string? ToolGuidance => LoadToolGuidance(config, Name);

    public JsonElement InputSchema => JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "theme": {
                    "type": "string",
                    "description": "A theme, motif, or symbolic element to search for across the logbook."
                }
            },
            "required": ["theme"]
        }
        """).RootElement;

    public async Task<string> ExecuteAsync(JsonElement input, CancellationToken ct)
    {
        var theme = input.GetProperty("theme").GetString()!;
        logger.LogInformation("find_thematic_connections invoked with theme: {Theme}", theme);

        var queryEmbedding = await embedding.EmbedAsync(theme, ct);

        var works = await vectorStore.SearchWorksAsync(queryEmbedding, TopK, ct);
        var entries = await vectorStore.SearchJournalEntriesAsync(queryEmbedding, TopK, ct);

        if (works.Count == 0 && entries.Count == 0)
            return "No thematic connections found in the logbook.";

        var sb = new StringBuilder();

        if (works.Count > 0)
        {
            sb.AppendLine("## Thematic patterns from your logbook");
            foreach (var work in works)
            {
                sb.AppendLine($"### {work.Title} ({work.Type}, {work.Status})");
                if (work.Notes.Themes is not null)
                    sb.AppendLine($"Themes: {work.Notes.Themes}");
                sb.AppendLine();
            }
        }

        if (entries.Count > 0)
        {
            sb.AppendLine("## Journal reflections on this theme");
            foreach (var entry in entries)
            {
                sb.AppendLine($"- [{entry.CreatedAt:yyyy-MM-dd}] {entry.RawContent}");
            }
        }

        return sb.ToString();
    }

    private static string? LoadToolGuidance(IConfiguration config, string toolName)
    {
        var path = config[$"Bedrock:ToolGuidancePaths:{toolName}"];
        if (path is null || !File.Exists(path)) return null;
        return File.ReadAllText(path).Trim();
    }
}
