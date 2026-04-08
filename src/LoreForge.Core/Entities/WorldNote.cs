using LoreForge.Core.Errors;
using LoreForge.Core.Primitives;

namespace LoreForge.Core.Entities;

public class WorldNote
{
    private WorldNote() { }

    public Guid Id { get; private set; }
    public WorldNoteCategory Category { get; private set; }
    public string Title { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public float[] Embedding { get; private set; } = [];
    public DateTime UpdatedAt { get; private set; }

    public static Result<WorldNote> Create(
        WorldNoteCategory category,
        string title,
        string content,
        float[] embedding)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<WorldNote>(WorldNoteErrors.TitleEmpty);

        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure<WorldNote>(WorldNoteErrors.ContentEmpty);

        return new WorldNote
        {
            Id = Guid.NewGuid(),
            Category = category,
            Title = title,
            Content = content,
            Embedding = embedding,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public Result Update(string content, float[] embedding)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure(WorldNoteErrors.ContentEmpty);

        Content = content;
        Embedding = embedding;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }
}

public enum WorldNoteCategory
{
    Character,
    Location,
    Magic,
    Lore,
    Plot,
    Freeform
}
