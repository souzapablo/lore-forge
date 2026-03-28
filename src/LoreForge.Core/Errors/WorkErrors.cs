using LoreForge.Core.Primitives;

namespace LoreForge.Core.Errors;

public static class WorkErrors
{
    public static Error NotFound(Guid id) => new("Work.NotFound", $"Work '{id}' does not exist.");
}
