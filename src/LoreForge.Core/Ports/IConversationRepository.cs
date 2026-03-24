using LoreForge.Core.Entities;

namespace LoreForge.Core.Ports;

public interface IConversationRepository
{
    Task<List<ConversationMessage>> GetHistoryAsync(string conversationId, CancellationToken ct);
    Task SaveMessageAsync(ConversationMessage message, CancellationToken ct);
    Task ClearHistoryAsync(string conversationId, CancellationToken ct);
}
