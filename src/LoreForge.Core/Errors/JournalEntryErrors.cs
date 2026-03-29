using LoreForge.Core.Primitives;

namespace LoreForge.Core.Errors;

public static class JournalEntryErrors
{
    public static readonly Error ContentEmpty =
        new("JournalEntry.ContentEmpty", "RawContent is required.", ErrorType.Validation);

    public static readonly Error FileRefRequired =
        new("JournalEntry.FileRefRequired", "FileRef is required when Source is file.", ErrorType.Validation);
}
