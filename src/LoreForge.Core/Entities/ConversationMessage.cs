namespace LoreForge.Core.Entities;

public class ConversationMessage
{
    public string ConversationId { get; set; } = default!;
    public long Timestamp { get; set; }
    public string Role { get; set; } = default!;
    public string Content { get; set; } = default!;
    public long TtlEpoch { get; set; }
}
