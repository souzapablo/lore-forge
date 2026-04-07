using LoreForge.Contracts.Common;
using LoreForge.Contracts.WorldNotes;
using LoreForge.Core.Entities;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.WorldNotes;

[Collection(PostgresCollection.Name)]
public class GetWorldNotesHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 200 OK with an empty list when no world notes exist")]
    public async Task Should_ReturnEmptyList_When_NoWorldNotesExist()
    {
        var response = await Client.GetAsync("/world-notes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorldNoteSummary>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact(DisplayName = "Returns all world notes when no category filter is applied")]
    public async Task Should_ReturnAllNotes_When_NoCategoryFilterApplied()
    {
        await Client.PutAsJsonAsync("/world-notes", MinimalRequest("Elyndra", WorldNoteCategory.Character));
        await Client.PutAsJsonAsync("/world-notes", MinimalRequest("The Ashfields", WorldNoteCategory.Location));

        var response = await Client.GetAsync("/world-notes");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorldNoteSummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, n => n.Title == "Elyndra");
        Assert.Contains(result.Items, n => n.Title == "The Ashfields");
    }

    [Fact(DisplayName = "Returns correct summary fields for a world note")]
    public async Task Should_ReturnCorrectFields_When_WorldNoteIsReturned()
    {
        await Client.PutAsJsonAsync("/world-notes", new
        {
            Title = "Elyndra",
            Category = WorldNoteCategory.Character,
            Content = "A wandering mage."
        });

        var response = await Client.GetAsync("/world-notes");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorldNoteSummary>>();
        var note = Assert.Single(result!.Items);

        Assert.Equal("Elyndra", note.Title);
        Assert.Equal(WorldNoteCategory.Character, note.Category);
        Assert.Equal("A wandering mage.", note.Content);
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.True(note.UpdatedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact(DisplayName = "Returns only notes matching the category filter")]
    public async Task Should_ReturnOnlyMatchingNotes_When_FilteredByCategory()
    {
        await Client.PutAsJsonAsync("/world-notes", MinimalRequest("Elyndra", WorldNoteCategory.Character));
        await Client.PutAsJsonAsync("/world-notes", MinimalRequest("The Ashfields", WorldNoteCategory.Location));

        var response = await Client.GetAsync($"/world-notes?category={(int)WorldNoteCategory.Character}");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorldNoteSummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, n => n.Title == "Elyndra");
        Assert.DoesNotContain(result.Items, n => n.Title == "The Ashfields");
    }

    [Fact(DisplayName = "Returns correct pagination metadata")]
    public async Task Should_ReturnCorrectPaginationMetadata_When_NotesArePaged()
    {
        for (var i = 1; i <= 15; i++)
            await Client.PutAsJsonAsync("/world-notes", MinimalRequest($"Note {i:D2}"));

        var response = await Client.GetAsync("/world-notes?page=1&pageSize=10");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorldNoteSummary>>();

        Assert.NotNull(result);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    private static object MinimalRequest(string title, WorldNoteCategory category = WorldNoteCategory.Lore) => new
    {
        Title = title,
        Category = category,
        Content = "some content"
    };
}
