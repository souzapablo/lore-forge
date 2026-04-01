using LoreForge.Core.Entities;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Logbook;

[Collection(PostgresCollection.Name)]
public class DeleteJournalEntryHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 204 No Content when a journal entry is deleted")]
    public async Task Should_Return204_When_JournalEntryIsDeleted()
    {
        var id = await CreateJournalEntry();

        var response = await Client.DeleteAsync($"/logbook/journal-entries/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(DisplayName = "Removes the journal entry from the database when deleted")]
    public async Task Should_RemoveJournalEntry_When_JournalEntryIsDeleted()
    {
        var id = await CreateJournalEntry();

        await Client.DeleteAsync($"/logbook/journal-entries/{id}");

        var saved = await Context.JournalEntries.FindAsync(id);
        Assert.Null(saved);
    }

    [Fact(DisplayName = "Returns 404 Not Found when the journal entry does not exist")]
    public async Task Should_Return404_When_JournalEntryDoesNotExist()
    {
        var response = await Client.DeleteAsync($"/logbook/journal-entries/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateJournalEntry()
    {
        var response = await Client.PostAsJsonAsync("/logbook/journal-entries", new
        {
            WorkId = (Guid?)null,
            ProgressSnapshot = (string?)null,
            Source = JournalSource.PlainText,
            RawContent = "Test journal entry content.",
            FileRef = (string?)null
        });

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
