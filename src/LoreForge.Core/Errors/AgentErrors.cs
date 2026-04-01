using LoreForge.Core.Primitives;

namespace LoreForge.Core.Errors;

public static class AgentErrors
{
    public static readonly Error MessageEmpty =
        new("Agent.MessageEmpty", "Message is required.", ErrorType.Validation);
}
