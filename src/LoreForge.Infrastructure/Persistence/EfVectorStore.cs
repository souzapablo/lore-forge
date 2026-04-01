using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace LoreForge.Infrastructure.Persistence;

public class EfVectorStore(LoreForgeDbContext db) : IVectorStore
{
    public Task<List<Work>> SearchWorksAsync(float[] queryEmbedding, int topK, CancellationToken ct)
    {
        var vector = new Vector(queryEmbedding);
        return db.Works
            .AsNoTracking()
            .OrderBy(w => w.Embedding.CosineDistance(vector))
            .Take(topK)
            .ToListAsync(ct);
    }

    public Task<List<JournalEntry>> SearchJournalEntriesAsync(float[] queryEmbedding, int topK, CancellationToken ct)
    {
        var vector = new Vector(queryEmbedding);
        return db.JournalEntries
            .AsNoTracking()
            .OrderBy(e => e.Embedding.CosineDistance(vector))
            .Take(topK)
            .ToListAsync(ct);
    }

    public Task<List<WorldNote>> SearchWorldNotesAsync(float[] queryEmbedding, int topK, CancellationToken ct)
    {
        var vector = new Vector(queryEmbedding);
        return db.WorldNotes
            .AsNoTracking()
            .OrderBy(n => n.Embedding.CosineDistance(vector))
            .Take(topK)
            .ToListAsync(ct);
    }
}
