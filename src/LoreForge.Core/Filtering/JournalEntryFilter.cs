using LoreForge.Core.Entities;

namespace LoreForge.Core.Filtering;

public record JournalEntryFilter(
    Guid? WorkId,
    JournalSource[]? Sources,
    DateTime? From,
    DateTime? To
);
