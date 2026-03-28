using LoreForge.Core.Entities;

namespace LoreForge.Core.Filtering;

public record WorkFilter(
    WorkType[]? Types,
    WorkStatus[]? Statuses,
    string[]? Genres,
    string[]? Tags
);
