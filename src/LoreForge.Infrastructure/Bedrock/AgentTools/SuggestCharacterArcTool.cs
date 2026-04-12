using System.Text;
using System.Text.Json;
using LoreForge.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LoreForge.Infrastructure.Bedrock.AgentTools;

public class SuggestCharacterArcTool(
    IEmbeddingService embedding,
    IVectorStore vectorStore,
    IConfiguration config,
    ILogger<SuggestCharacterArcTool> logger) : IAgentTool
{
    private const int TopK = 5;

    public string Name => "suggest_character_arc";

    public string Description =>
        "Searches the user's logbook and journal entries for existing character references, " +
        "emotional arcs, and growth patterns in consumed media to surface raw material for character arc suggestions.";

    public string? ToolGuidance => LoadToolGuidance(config, Name);

    public JsonElement InputSchema => JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "character_description": {
                    "type": "string",
                    "description": "A description of the character whose arc to develop."
                }
            },
            "required": ["character_description"]
        }
        """).RootElement;

    public async Task<string> ExecuteAsync(JsonElement input, CancellationToken ct)
    {
        var characterDescription = input.GetProperty("character_description").GetString()!;
        logger.LogInformation("suggest_character_arc invoked with character description: {CharacterDescription}", characterDescription);

        var queryEmbedding = await embedding.EmbedAsync(characterDescription, ct);

        var works = await vectorStore.SearchWorksAsync(queryEmbedding, TopK, ct);
        var entries = await vectorStore.SearchJournalEntriesAsync(queryEmbedding, TopK, ct);

        if (works.Count == 0 && entries.Count == 0)
            return "No relevant character arc references found in the logbook.";

        var sb = new StringBuilder();

        if (works.Count > 0)
        {
            sb.AppendLine("## Character arcs from your logbook");
            foreach (var work in works)
            {
                sb.AppendLine($"### {work.Title} ({work.Type}, {work.Status})");
                if (work.Notes.Characters is not null)
                    sb.AppendLine($"Characters: {work.Notes.Characters}");
                if (work.Notes.Themes is not null)
                    sb.AppendLine($"Themes: {work.Notes.Themes}");
                sb.AppendLine();
            }
        }

        if (entries.Count > 0)
        {
            sb.AppendLine("## Journal observations on characters");
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
