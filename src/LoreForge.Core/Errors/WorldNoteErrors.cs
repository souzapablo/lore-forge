using LoreForge.Core.Primitives;

namespace LoreForge.Core.Errors;

public static class WorldNoteErrors
{
    public static readonly Error TitleEmpty =
        new("WorldNote.TitleEmpty", "Title is required.", ErrorType.Validation);

    public static readonly Error ContentEmpty =
        new("WorldNote.ContentEmpty", "Content is required.", ErrorType.Validation);

    public static Error NotFound(Guid id) =>
        new("WorldNote.NotFound", $"World note with id '{id}' was not found.", ErrorType.NotFound);
}