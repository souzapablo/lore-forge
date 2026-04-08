using LoreForge.Core.Errors;
using LoreForge.Core.Primitives;
using System.Text.Json;

namespace LoreForge.Core.Entities;

public class Work
{
    private Work() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public WorkType Type { get; private set; }
    public string[] Genres { get; private set; } = [];
    public WorkStatus Status { get; private set; }
    public JsonDocument? Progress { get; private set; }
    public WorkNotes Notes { get; private set; } = new();
    public string[] Tags { get; private set; } = [];
    public float[] Embedding { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }

    public static Result<Work> Create(
        string title,
        WorkType type,
        string[] genres,
        WorkStatus status,
        JsonDocument? progress,
        WorkNotes notes,
        string[] tags,
        float[] embedding)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<Work>(WorkErrors.TitleEmpty);

        if (!notes.HasContent)
            return Result.Failure<Work>(WorkErrors.NotesEmpty);

        return new Work
        {
            Id = Guid.NewGuid(),
            Title = title,
            Type = type,
            Genres = genres,
            Status = status,
            Progress = progress,
            Notes = notes,
            Tags = tags,
            Embedding = embedding
        };
    }

    public Result Update(
        string title,
        string[] genres,
        WorkStatus status,
        JsonDocument? progress,
        WorkNotes notes,
        string[] tags,
        float[] embedding)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure(WorkErrors.TitleEmpty);

        if (!notes.HasContent)
            return Result.Failure(WorkErrors.NotesEmpty);

        Title = title;
        Genres = genres;
        Status = status;
        Progress = progress;
        Notes = notes;
        Tags = tags;
        Embedding = embedding;

        return Result.Success();
    }
}

public class WorkNotes
{
    public string? Worldbuilding { get; init; }
    public string? Magic { get; init; }
    public string? Characters { get; init; }
    public string? Themes { get; init; }
    public string? PlotStructure { get; init; }
    public string? WhatILiked { get; init; }

    public bool HasContent =>
        Worldbuilding is not null || Magic is not null || Characters is not null ||
        Themes is not null || PlotStructure is not null || WhatILiked is not null;

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
