using LoreForge.Contracts.WorldNotes;
using LoreForge.Core.Entities;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.WorldNotes;

[Collection(PostgresCollection.Name)]
public class GetWorldNoteByIdHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 200 OK with the world note when it exists")]
    public async Task Should_Return200WithNote_When_WorldNoteExists()
    {
        var id = await CreateWorldNote("Elyndra", WorldNoteCategory.Character, "A wandering mage.");

        var response = await Client.GetAsync($"/world-notes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var note = await response.Content.ReadFromJsonAsync<WorldNoteSummary>();
        Assert.NotNull(note);
        Assert.Equal(id, note.Id);
        Assert.Equal("Elyndra", note.Title);
        Assert.Equal(WorldNoteCategory.Character, note.Category);
        Assert.Equal("A wandering mage.", note.Content);
        Assert.True(note.UpdatedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact(DisplayName = "Returns 404 Not Found when the world note does not exist")]
    public async Task Should_Return404_When_WorldNoteDoesNotExist()
    {
        var response = await Client.GetAsync($"/world-notes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateWorldNote(
        string title = "Test Note",
        WorldNoteCategory category = WorldNoteCategory.Lore,
        string content = "Some content")
    {
        var response = await Client.PutAsJsonAsync("/world-notes", new { Title = title, Category = category, Content = content });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
