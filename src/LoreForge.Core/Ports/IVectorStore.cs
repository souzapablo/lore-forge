using LoreForge.Core.Entities;

namespace LoreForge.Core.Ports;

public interface IVectorStore
{
    Task<List<Work>> SearchWorksAsync(float[] queryEmbedding, int topK, CancellationToken ct);
    Task<List<JournalEntry>> SearchJournalEntriesAsync(float[] queryEmbedding, int topK, CancellationToken ct);
    Task<List<WorldNote>> SearchWorldNotesAsync(float[] queryEmbedding, int topK, CancellationToken ct);
}
