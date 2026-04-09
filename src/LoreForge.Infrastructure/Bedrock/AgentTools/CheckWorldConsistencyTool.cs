using System.Text;
using System.Text.Json;
using LoreForge.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LoreForge.Infrastructure.Bedrock.AgentTools;

public class CheckWorldConsistencyTool(
    IEmbeddingService embedding,
    IVectorStore vectorStore,
    IConfiguration config,
    ILogger<CheckWorldConsistencyTool> logger
) : IAgentTool
{
    private const int TopK = 5;

    private readonly string _webBaseUrl = (config["WebBaseUrl"] ?? "").TrimEnd('/');

    public string Name => "check_world_consistency";

    public string Description =>
        "Searches the user's WorldNotes to verify whether a story element (character trait, location detail, " +
        "magic rule, etc.) is consistent with established lore. Use this to catch contradictions.";

    public string? ToolGuidance => LoadToolGuidance(config, Name);


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
        logger.LogInformation("check_world_consistency invoked with query: {Query}", query);

        var queryEmbedding = await embedding.EmbedAsync(query, ct);
        var notes = await vectorStore.SearchWorldNotesAsync(queryEmbedding, TopK, ct);

        if (notes.Count == 0)
            return "No relevant world notes found for this consistency check.";

        var sb = new StringBuilder();
        sb.AppendLine("## Established world lore");

        foreach (var note in notes)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Note retrieved: {Title} [{Category}]\n{Content}", note.Title, note.Category, note.Content);
            sb.AppendLine($"Title: {note.Title}");
            sb.AppendLine($"Category: {note.Category}");
            sb.AppendLine($"URL: {_webBaseUrl}/world-notes/{note.Id}");
            sb.AppendLine($"Content: {note.Content}");
            sb.AppendLine();
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
