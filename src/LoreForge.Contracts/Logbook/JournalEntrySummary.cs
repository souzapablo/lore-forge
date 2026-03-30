using LoreForge.Core.Entities;

namespace LoreForge.Contracts.Logbook;

public record JournalEntrySummary(
    Guid Id,
    Guid? WorkId,
    string? ProgressSnapshot,
    JournalSource Source,
    string RawContent,
    DateTime CreatedAt
);
