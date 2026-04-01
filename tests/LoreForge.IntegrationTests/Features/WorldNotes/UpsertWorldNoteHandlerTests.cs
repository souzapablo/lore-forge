using LoreForge.Core.Entities;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.WorldNotes;

[Collection(PostgresCollection.Name)]
public class UpsertWorldNoteHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 200 OK with id when a world note is created")]
    public async Task Should_Return200WithId_When_WorldNoteIsCreated()
    {
        var response = await Client.PutAsJsonAsync("/world-notes", MinimalRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact(DisplayName = "Persists all world note properties to the database when a world note is created")]
    public async Task Should_PersistAllProperties_When_WorldNoteIsCreated()
    {
        var request = new
        {
            Title = "Disco Elysium",
            Content = "I really enjoy the way politics is part of the game.",
            Category = WorldNoteCategory.Lore,
        };

        var response = await Client.PutAsJsonAsync("/world-notes", request);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var saved = await Context.WorldNotes.FindAsync(id);

        Assert.NotNull(saved);
        Assert.Equal("Disco Elysium", saved.Title);
        Assert.Equal(WorldNoteCategory.Lore, saved.Category);
        Assert.Equal("I really enjoy the way politics is part of the game.", saved.Content);
        Assert.True(saved.UpdatedAt > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact(DisplayName = "Updates content and embedding when a note with the same title and category already exists")]
    public async Task Should_UpdateContentAndEmbedding_When_NoteWithSameTitleAndCategoryExists()
    {
        var request = MinimalRequest();
        var firstResponse = await Client.PutAsJsonAsync("/world-notes", request);
        var originalId = await firstResponse.Content.ReadFromJsonAsync<Guid>();

        var updatedRequest = request with { Content = "Updated content." };
        var secondResponse = await Client.PutAsJsonAsync("/world-notes", updatedRequest);
        var returnedId = await secondResponse.Content.ReadFromJsonAsync<Guid>();

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(originalId, returnedId);

        var saved = await Context.WorldNotes.FindAsync(returnedId);
        Assert.NotNull(saved);
        Assert.Equal("Updated content.", saved.Content);
        Assert.Equal(1, Context.WorldNotes.Count(n => n.Title == request.Title && n.Category == request.Category));
    }

    [Fact(DisplayName = "Persists the embedding vector returned by the embedding service")]
    public async Task Should_PersistEmbedding_When_WorldNoteIsCreated()
    {
        var response = await Client.PutAsJsonAsync("/world-notes", MinimalRequest());
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var saved = await Context.WorldNotes.FindAsync(id);

        Assert.NotNull(saved);
        Assert.NotEmpty(saved.Embedding);
        await EmbeddingService.Received(1)
            .EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Returns 422 when Title is empty")]
    public async Task Should_Return422_When_TitleIsEmpty()
    {
        var request = MinimalRequest() with { Title = "" };

        var response = await Client.PutAsJsonAsync("/world-notes", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "Returns 422 when Content is empty")]
    public async Task Should_Return422_When_ContentIsEmpty()
    {
        var request = MinimalRequest() with { Content = "" };

        var response = await Client.PutAsJsonAsync("/world-notes", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private record WorldNoteRequest(WorldNoteCategory Category, string Title, string Content);
    private static WorldNoteRequest MinimalRequest(string title = "Test WorldNote") =>
        new(
            Title: title,
            Category: WorldNoteCategory.Lore,
            Content: "some content");
}
