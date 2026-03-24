namespace LoreForge.Core.Ports;

public interface IAgentService
{
    Task<string> ChatAsync(string conversationId, string userMessage, CancellationToken ct);
}
