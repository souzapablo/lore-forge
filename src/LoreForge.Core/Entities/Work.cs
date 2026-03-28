using System.Text.Json;

namespace LoreForge.Core.Entities;

public class Work
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public WorkType Type { get; set; }
    public string[] Genres { get; set; } = [];
    public WorkStatus Status { get; set; }
    public JsonDocument? Progress { get; set; }
    public WorkNotes Notes { get; set; } = new();
    public string[] Tags { get; set; } = [];
    public float[] Embedding { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class WorkNotes
{
    public string? Worldbuilding { get; set; }
    public string? Magic { get; set; }
    public string? Characters { get; set; }
    public string? Themes { get; set; }
    public string? PlotStructure { get; set; }
    public string? WhatILiked { get; set; }

    public string ToEmbeddingText(string title)
    {
        var parts = new List<string> { title };
        if (Worldbuilding is not null) parts.Add($"Worldbuilding: {Worldbuilding}");
        if (Magic is not null) parts.Add($"Magic: {Magic}");
        if (Characters is not null) parts.Add($"Characters: {Characters}");
        if (Themes is not null) parts.Add($"Themes: {Themes}");
        if (PlotStructure is not null) parts.Add($"Plot structure: {PlotStructure}");
        if (WhatILiked is not null) parts.Add($"What I liked: {WhatILiked}");
        return string.Join("\n", parts);
    }
}

public enum WorkType
{
    Game,
    Book,
    Movie,
    Series,
    Other
}

public enum WorkStatus
{
    InProgress,
    Completed,
    Dropped
}
