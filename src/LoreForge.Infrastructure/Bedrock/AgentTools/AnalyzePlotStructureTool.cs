using System.Text;
using System.Text.Json;
using LoreForge.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LoreForge.Infrastructure.Bedrock.AgentTools;

public class AnalyzePlotStructureTool(
    IEmbeddingService embedding,
    IVectorStore vectorStore,
    IConfiguration config,
    ILogger<AnalyzePlotStructureTool> logger) : IAgentTool
{
    private const int TopK = 5;

    private readonly string _webBaseUrl = (config["WebBaseUrl"] ?? "").TrimEnd('/');

    public string Name => "analyze_plot_structure";

    public string Description =>
        "Searches the user's WorldNotes (structured lore) and JournalEntries (freeform observations) " +
        "to surface grounded context for examining plot structure, act breaks, pacing, and missing connective tissue.";

    public string? ToolGuidance => LoadToolGuidance(config, Name);

    public JsonElement InputSchema => JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "plot_question": {
                    "type": "string",
                    "description": "A question or description about the plot structure to investigate."
                }
            },
            "required": ["plot_question"]
        }
        """).RootElement;

    public async Task<string> ExecuteAsync(JsonElement input, CancellationToken ct)
    {
        var plotQuestion = input.GetProperty("plot_question").GetString()!;
        logger.LogInformation("analyze_plot_structure invoked with plot question: {PlotQuestion}", plotQuestion);

        var queryEmbedding = await embedding.EmbedAsync(plotQuestion, ct);

        var notes = await vectorStore.SearchWorldNotesAsync(queryEmbedding, TopK, ct);
        var entries = await vectorStore.SearchJournalEntriesAsync(queryEmbedding, TopK, ct);

        if (notes.Count == 0 && entries.Count == 0)
            return "No relevant plot structure references found.";

        var sb = new StringBuilder();

        if (notes.Count > 0)
        {
            sb.AppendLine("## Relevant world lore");
            foreach (var note in notes)
            {
                sb.AppendLine($"### {note.Title} [{note.Category}]");
                sb.AppendLine($"URL: {_webBaseUrl}/world-notes/{note.Id}");
                sb.AppendLine(note.Content);
                sb.AppendLine();
            }
        }

        if (entries.Count > 0)
        {
            sb.AppendLine("## Journal observations on plot");
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
