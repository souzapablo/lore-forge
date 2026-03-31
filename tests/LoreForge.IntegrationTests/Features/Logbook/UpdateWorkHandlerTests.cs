using LoreForge.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Logbook;

[Collection(PostgresCollection.Name)]
public class UpdateWorkHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 204 No Content when update succeeds")]
    public async Task Should_Return204_When_UpdateSucceeds()
    {
        var id = await CreateWorkAsync();

        var response = await Client.PutAsJsonAsync($"/logbook/works/{id}", FullUpdateRequest());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(DisplayName = "Returns 404 when work does not exist")]
    public async Task Should_Return404_When_WorkDoesNotExist()
    {
        var response = await Client.PutAsJsonAsync($"/logbook/works/{Guid.NewGuid()}", FullUpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "Returns 422 when Title is empty")]
    public async Task Should_Return422_When_TitleIsEmpty()
    {
        var id = await CreateWorkAsync();
        var request = FullUpdateRequest() with { Title = "" };

        var response = await Client.PutAsJsonAsync($"/logbook/works/{id}", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "Returns 422 when all Notes fields are null")]
    public async Task Should_Return422_When_AllNotesAreNull()
    {
        var id = await CreateWorkAsync();
        var request = FullUpdateRequest() with { Notes = new(null, null, null, null, null, null) };

        var response = await Client.PutAsJsonAsync($"/logbook/works/{id}", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "Persists updated fields to the database")]
    public async Task Should_PersistUpdatedFields_When_UpdateSucceeds()
    {
        var id = await CreateWorkAsync();
        var request = new UpdateWorkRequest(
            Title: "Dune",
            Genres: ["sci-fi", "epic"],
            Status: WorkStatus.Completed,
            Progress: null,
            Notes: new(
                Worldbuilding: "Desert planet Arrakis",
                Magic: null,
                Characters: "Paul Atreides",
                Themes: "Power and religion",
                PlotStructure: null,
                WhatILiked: "Political intrigue"),
            Tags: ["classic", "sci-fi"]
        );

        await Client.PutAsJsonAsync($"/logbook/works/{id}", request);

        var saved = await Context.Works.AsNoTracking().FirstAsync(w => w.Id == id);
        Assert.Equal("Dune", saved.Title);
        Assert.Equal(["sci-fi", "epic"], saved.Genres);
        Assert.Equal(WorkStatus.Completed, saved.Status);
        Assert.Equal(["classic", "sci-fi"], saved.Tags);
        Assert.Equal("Desert planet Arrakis", saved.Notes.Worldbuilding);
        Assert.Null(saved.Notes.Magic);
        Assert.Equal("Paul Atreides", saved.Notes.Characters);
        Assert.Equal("Power and religion", saved.Notes.Themes);
        Assert.Null(saved.Notes.PlotStructure);
        Assert.Equal("Political intrigue", saved.Notes.WhatILiked);
    }

    [Fact(DisplayName = "Re-embeds when notes change")]
    public async Task Should_ReEmbed_When_NotesChange()
    {
        var id = await CreateWorkAsync();
        EmbeddingService.ClearReceivedCalls();

        await Client.PutAsJsonAsync($"/logbook/works/{id}", FullUpdateRequest());

        await EmbeddingService.Received(1)
            .EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private async Task<Guid> CreateWorkAsync()
    {
        var response = await Client.PostAsJsonAsync("/logbook/works", new
        {
            Title = "Elden Ring",
            Type = WorkType.Game,
            Genres = new[] { "action", "rpg" },
            Status = WorkStatus.InProgress,
            Progress = (object?)null,
            Notes = new { Worldbuilding = "The Lands Between", Magic = (string?)null, Characters = (string?)null, Themes = (string?)null, PlotStructure = (string?)null, WhatILiked = (string?)null },
            Tags = Array.Empty<string>()
        });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private record UpdateWorkNotesRequest(
        string? Worldbuilding, string? Magic, string? Characters,
        string? Themes, string? PlotStructure, string? WhatILiked);

    private record UpdateWorkRequest(
        string Title, string[] Genres, WorkStatus Status,
        object? Progress, UpdateWorkNotesRequest Notes, string[] Tags);

    private static UpdateWorkRequest FullUpdateRequest() =>
        new(
            Title: "Elden Ring — Updated",
            Genres: ["action", "rpg"],
            Status: WorkStatus.Completed,
            Progress: null,
            Notes: new("Updated world", null, "Updated chars", null, null, null),
            Tags: ["fromsoft"]
        );
}
