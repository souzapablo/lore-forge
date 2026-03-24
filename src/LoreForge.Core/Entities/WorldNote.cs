namespace LoreForge.Core.Entities;

public class WorldNote
{
    public Guid Id { get; set; }
    public WorldNoteCategory Category { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public float[] Embedding { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
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
