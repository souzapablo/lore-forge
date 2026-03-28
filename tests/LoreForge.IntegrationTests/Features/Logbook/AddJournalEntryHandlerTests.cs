using LoreForge.Core.Entities;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Logbook;

[Collection(PostgresCollection.Name)]
public class AddJournalEntryHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 200 OK with the new entry's ID when a journal entry is added")]
    public async Task Should_Return200WithId_When_JournalEntryIsAdded()
    {
        var response = await Client.PostAsJsonAsync("/logbook/journal-entries", MinimalRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact(DisplayName = "Persists all journal entry properties to the database when an entry is added")]
    public async Task Should_PersistAllProperties_When_JournalEntryIsAdded()
    {
        var workResponse = await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest());
        var workId = await workResponse.Content.ReadFromJsonAsync<Guid>();

        var request = new
        {
            WorkId = workId,
            ProgressSnapshot = "Chapter 3",
            Source = JournalSource.PlainText,
            RawContent = "Really enjoyed the world design in this section.",
            FileRef = (string?)null
        };

        var response = await Client.PostAsJsonAsync("/logbook/journal-entries", request);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var saved = await Context.JournalEntries.FindAsync(id);

        Assert.NotNull(saved);
        Assert.Equal(workId, saved.WorkId);
        Assert.Equal("Chapter 3", saved.ProgressSnapshot);
        Assert.Equal(JournalSource.PlainText, saved.Source);
        Assert.Equal("Really enjoyed the world design in this section.", saved.RawContent);
        Assert.Null(saved.FileRef);
    }

    [Fact(DisplayName = "Persists the embedding of RawContent when a journal entry is added")]
    public async Task Should_PersistEmbedding_When_JournalEntryIsAdded()
    {
        var response = await Client.PostAsJsonAsync("/logbook/journal-entries", MinimalRequest());
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var saved = await Context.JournalEntries.FindAsync(id);

        Assert.NotNull(saved);
        Assert.NotEmpty(saved.Embedding);
        await EmbeddingService.Received(1)
            .EmbedAsync(Arg.Is<string>(s => s == MinimalRequest().RawContent), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Returns 404 Not Found when the provided WorkId does not exist")]
    public async Task Should_Return404_When_WorkIdDoesNotExist()
    {
        var request = MinimalRequest() with { WorkId = Guid.NewGuid() };

        var response = await Client.PostAsJsonAsync("/logbook/journal-entries", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "Allows a standalone journal entry with no WorkId")]
    public async Task Should_PersistEntry_When_WorkIdIsNull()
    {
        var request = MinimalRequest() with { WorkId = null };

        var response = await Client.PostAsJsonAsync("/logbook/journal-entries", request);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var saved = await Context.JournalEntries.FindAsync(id);

        Assert.NotNull(saved);
        Assert.Null(saved.WorkId);
    }

    private record JournalEntryRequest(
        Guid? WorkId, string? ProgressSnapshot, JournalSource Source,
        string RawContent, string? FileRef);

    private static JournalEntryRequest MinimalRequest() =>
        new(
            WorkId: null,
            ProgressSnapshot: null,
            Source: JournalSource.PlainText,
            RawContent: "Test journal entry content.",
            FileRef: null);

    private static object MinimalWorkRequest() => new
    {
        Title = "Test Work",
        Type = 0,
        Genres = Array.Empty<string>(),
        Status = 0,
        Progress = (object?)null,
        Notes = new { Worldbuilding = (string?)null, Magic = (string?)null, Characters = (string?)null, Themes = (string?)null, PlotStructure = (string?)null, WhatILiked = (string?)null },
        Tags = Array.Empty<string>()
    };
}
