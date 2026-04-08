using LoreForge.Core.Errors;
using LoreForge.Core.Primitives;

namespace LoreForge.Core.Entities;

public class JournalEntry
{
    private JournalEntry() { }

    public Guid Id { get; private set; }
    public Guid? WorkId { get; private set; }
    public string? ProgressSnapshot { get; private set; }
    public JournalSource Source { get; private set; }
    public string RawContent { get; private set; } = default!;
    public string? FileRef { get; private set; }
    public float[] Embedding { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }

    public static Result<JournalEntry> Create(
        Guid? workId,
        string? progressSnapshot,
        JournalSource source,
        string rawContent,
        string? fileRef,
        float[] embedding)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return Result.Failure<JournalEntry>(JournalEntryErrors.ContentEmpty);

        if (source == JournalSource.File && string.IsNullOrWhiteSpace(fileRef))
            return Result.Failure<JournalEntry>(JournalEntryErrors.FileRefRequired);

        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            ProgressSnapshot = progressSnapshot,
            Source = source,
            RawContent = rawContent,
            FileRef = fileRef,
            Embedding = embedding,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum JournalSource
{
    Chat,
    PlainText,
    File
}
