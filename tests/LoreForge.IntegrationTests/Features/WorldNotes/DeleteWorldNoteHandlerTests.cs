using LoreForge.Core.Entities;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.WorldNotes;

[Collection(PostgresCollection.Name)]
public class DeleteWorldNoteHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 204 No Content when a world note is deleted")]
    public async Task Should_Return204_When_WorldNoteIsDeleted()
    {
        var id = await CreateWorldNote();

        var response = await Client.DeleteAsync($"/world-notes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(DisplayName = "Removes the world note from the database when deleted")]
    public async Task Should_RemoveWorldNote_When_WorldNoteIsDeleted()
    {
        var id = await CreateWorldNote();

        await Client.DeleteAsync($"/world-notes/{id}");

        var saved = await Context.WorldNotes.FindAsync(id);
        Assert.Null(saved);
    }

    [Fact(DisplayName = "Returns 404 Not Found when the world note does not exist")]
    public async Task Should_Return404_When_WorldNoteDoesNotExist()
    {
        var response = await Client.DeleteAsync($"/world-notes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateWorldNote()
    {
        var response = await Client.PutAsJsonAsync("/world-notes", new
        {
            Title = "Test Note",
            Category = WorldNoteCategory.Lore,
            Content = "Some content"
        });

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
