using LoreForge.Core.Entities;
using LoreForge.Infrastructure.Persistence;

namespace LoreForge.IntegrationTests.Infrastructure;

[Collection(PostgresCollection.Name)]
public class EfVectorStoreTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private EfVectorStore Store => new(Context);

    [Fact(DisplayName = "Returns works ordered by similarity to the query embedding")]
    public async Task Should_ReturnWorksOrderedBySimilarity_When_WorksExist()
    {
        var workA = SeedWork("Work A", EmbeddingAt(0));
        var workB = SeedWork("Work B", EmbeddingAt(1));
        var workC = SeedWork("Work C", EmbeddingAt(2));
        Context.Works.AddRange(workA, workB, workC);
        await Context.SaveChangesAsync();

        var results = await Store.SearchWorksAsync(EmbeddingAt(0), topK: 3, CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.Equal(workA.Id, results[0].Id);
    }

    [Fact(DisplayName = "Returns at most topK works")]
    public async Task Should_ReturnAtMostTopK_When_MoreWorksThanTopK()
    {
        Context.Works.AddRange(
            SeedWork("A", EmbeddingAt(0)),
            SeedWork("B", EmbeddingAt(1)),
            SeedWork("C", EmbeddingAt(2)));
        await Context.SaveChangesAsync();

        var results = await Store.SearchWorksAsync(EmbeddingAt(0), topK: 2, CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact(DisplayName = "Returns journal entries ordered by similarity to the query embedding")]
    public async Task Should_ReturnJournalEntriesOrderedBySimilarity_When_EntriesExist()
    {
        var entryA = SeedEntry("Content A", EmbeddingAt(0));
        var entryB = SeedEntry("Content B", EmbeddingAt(1));
        Context.JournalEntries.AddRange(entryA, entryB);
        await Context.SaveChangesAsync();

        var results = await Store.SearchJournalEntriesAsync(EmbeddingAt(0), topK: 2, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(entryA.Id, results[0].Id);
    }

    [Fact(DisplayName = "Returns empty list when no works exist")]
    public async Task Should_ReturnEmptyList_When_NoWorksExist()
    {
        var results = await Store.SearchWorksAsync(EmbeddingAt(0), topK: 5, CancellationToken.None);

        Assert.Empty(results);
    }

    private static float[] EmbeddingAt(int dimension)
    {
        var v = new float[1024];
        v[dimension] = 1.0f;
        return v;
    }

    private static Work SeedWork(string title, float[] embedding) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Type = WorkType.Book,
        Genres = [],
        Status = WorkStatus.InProgress,
        Tags = [],
        Notes = new WorkNotes { Worldbuilding = "Some world" },
        Embedding = embedding
    };

    private static JournalEntry SeedEntry(string content, float[] embedding) => new()
    {
        Id = Guid.NewGuid(),
        Source = JournalSource.PlainText,
        RawContent = content,
        Embedding = embedding
    };
}
