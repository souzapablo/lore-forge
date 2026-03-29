using LoreForge.Core.Primitives;

namespace LoreForge.Core.Errors;

public static class WorkErrors
{
    public static Error NotFound(Guid id) =>
        new("Work.NotFound", $"Work '{id}' does not exist.", ErrorType.NotFound);

    public static readonly Error TitleEmpty =
        new("Work.TitleEmpty", "Title is required.", ErrorType.Validation);

    public static readonly Error NotesEmpty =
        new("Work.NotesEmpty", "At least one notes field must be provided.", ErrorType.Validation);
}

