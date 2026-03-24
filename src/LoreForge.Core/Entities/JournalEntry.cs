namespace LoreForge.Core.Entities;

public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid? WorkId { get; set; }
    public string? ProgressSnapshot { get; set; }
    public JournalSource Source { get; set; }
    public string RawContent { get; set; } = default!;
    public string? FileRef { get; set; }
    public float[] Embedding { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public enum JournalSource
{
    Chat,
    PlainText,
    File
}
