using LoreForge.Core.Entities;

namespace LoreForge.Contracts.Logbook;

public record WorkSummary(
    Guid Id,
    string Title,
    WorkType Type,
    string[] Genres,
    WorkStatus Status,
    string[] Tags,
    DateTime CreatedAt
);
