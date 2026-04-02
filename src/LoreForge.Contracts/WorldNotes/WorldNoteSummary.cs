using LoreForge.Core.Entities;

namespace LoreForge.Contracts.WorldNotes;

public record WorldNoteSummary(
    Guid Id,
    WorldNoteCategory Category,
    string Title,
    string Content,
    DateTime UpdatedAt
);
