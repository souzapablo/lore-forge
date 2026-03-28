using LoreForge.Api.Features.Logbook;
using LoreForge.Core.Entities;
using LoreForge.Core.Primitives;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Logbook;

[Collection(PostgresCollection.Name)]
public class GetJournalEntriesHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 200 OK with an empty list when no entries exist")]
    public async Task Should_ReturnEmptyList_When_NoEntriesExist()
    {
        var response = await Client.GetAsync("/logbook/journal-entries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<JournalEntrySummary>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact(DisplayName = "Returns all entries when no filters are applied")]
    public async Task Should_ReturnAllEntries_When_NoFiltersApplied()
    {
        await CreateEntry("First entry");
        await CreateEntry("Second entry");

        var response = await Client.GetAsync("/logbook/journal-entries");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<JournalEntrySummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, e => e.RawContent == "First entry");
        Assert.Contains(result.Items, e => e.RawContent == "Second entry");
    }

    [Fact(DisplayName = "Returns entries ordered by most recent first")]
    public async Task Should_ReturnEntriesOrderedByCreatedAtDescending_When_EntriesExist()
    {
        await CreateEntry("First entry");
        await CreateEntry("Second entry");

        var response = await Client.GetAsync("/logbook/journal-entries");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<JournalEntrySummary>>();

        Assert.NotNull(result);
        Assert.Equal("Second entry", result.Items[0].RawContent);
        Assert.Equal("First entry", result.Items[1].RawContent);
    }

    [Fact(DisplayName = "Filters entries by WorkId")]
    public async Task Should_ReturnOnlyMatchingEntries_When_FilteredByWorkId()
    {
        var workId = await CreateWork();
        await CreateEntry("Linked entry", workId: workId);
        await CreateEntry("Standalone entry");

        var response = await Client.GetAsync($"/logbook/journal-entries?workId={workId}");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<JournalEntrySummary>>();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Linked entry", result.Items[0].RawContent);
    }

    [Fact(DisplayName = "Filters entries by source")]
    public async Task Should_ReturnOnlyMatchingEntries_When_FilteredBySource()
    {
        await CreateEntry("Plain text entry", source: JournalSource.PlainText);
        await CreateEntry("Chat entry", source: JournalSource.Chat);

        var response = await Client.GetAsync("/logbook/journal-entries?sources=1");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<JournalEntrySummary>>();

        Assert.NotNull(result);
        Assert.All(result.Items, e => Assert.Equal(JournalSource.PlainText, e.Source));
    }

    [Fact(DisplayName = "Returns correct pagination metadata")]
    public async Task Should_ReturnCorrectPaginationMetadata_When_EntriesArePaged()
    {
        for (var i = 1; i <= 15; i++)
            await CreateEntry($"Entry {i}");

        var response = await Client.GetAsync("/logbook/journal-entries?page=1&pageSize=10");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<JournalEntrySummary>>();

        Assert.NotNull(result);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    private async Task CreateEntry(
        string rawContent,
        Guid? workId = null,
        JournalSource source = JournalSource.PlainText)
    {
        await Client.PostAsJsonAsync("/logbook/journal-entries", new
        {
            WorkId = workId,
            ProgressSnapshot = (string?)null,
            Source = source,
            RawContent = rawContent,
            FileRef = (string?)null
        });
    }

    private async Task<Guid> CreateWork()
    {
        var response = await Client.PostAsJsonAsync("/logbook/works", new
        {
            Title = "Test Work",
            Type = WorkType.Book,
            Genres = Array.Empty<string>(),
            Status = WorkStatus.InProgress,
            Progress = (object?)null,
            Notes = new { Worldbuilding = (string?)null, Magic = (string?)null, Characters = (string?)null, Themes = (string?)null, PlotStructure = (string?)null, WhatILiked = (string?)null },
            Tags = Array.Empty<string>()
        });

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
