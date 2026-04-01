namespace LoreForge.Core.Entities;

public class ConversationSummary
{
    public string ConversationId { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public long CreatedAt { get; set; }
}
