using LoreForge.Core.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Logbook;

[Collection(PostgresCollection.Name)]
public class AddWorkHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 201 Created with location header when a work is added")]
    public async Task Should_Return201WithLocation_When_WorkIsAdded()
    {
        var response = await Client.PostAsJsonAsync("/logbook/works", MinimalRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"/logbook/works/{id}", response.Headers.Location?.OriginalString);
    }

    [Fact(DisplayName = "Persists all work properties to the database when a work is added")]
    public async Task Should_PersistAllProperties_When_WorkIsAdded()
    {
        var request = new
        {
            Title = "Elden Ring",
            Type = WorkType.Game,
            Genres = new[] { "action", "rpg" },
            Status = WorkStatus.Completed,
            Progress = (object?)null,
            Notes = new
            {
                Worldbuilding = "The Lands Between",
                Magic = "Elden Ring shards",
                Characters = "Tarnished, Melina",
                Themes = "Death and renewal",
                PlotStructure = "Open world boss rush",
                WhatILiked = "Incredible world design"
            },
            Tags = new[] { "fromsoft", "souls" }
        };

        var response = await Client.PostAsJsonAsync("/logbook/works", request);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var saved = await Context.Works.FindAsync(id);

        Assert.NotNull(saved);
        Assert.Equal("Elden Ring", saved.Title);
        Assert.Equal(WorkType.Game, saved.Type);
        Assert.Equal(["action", "rpg"], saved.Genres);
        Assert.Equal(WorkStatus.Completed, saved.Status);
        Assert.Equal(["fromsoft", "souls"], saved.Tags);
        Assert.Equal("The Lands Between", saved.Notes.Worldbuilding);
        Assert.Equal("Elden Ring shards", saved.Notes.Magic);
        Assert.Equal("Tarnished, Melina", saved.Notes.Characters);
        Assert.Equal("Death and renewal", saved.Notes.Themes);
        Assert.Equal("Open world boss rush", saved.Notes.PlotStructure);
        Assert.Equal("Incredible world design", saved.Notes.WhatILiked);
    }

    [Fact(DisplayName = "Persists the embedding vector returned by the embedding service")]
    public async Task Should_PersistEmbedding_When_WorkIsAdded()
    {
        var response = await Client.PostAsJsonAsync("/logbook/works", MinimalRequest());
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var saved = await Context.Works.FindAsync(id);

        Assert.NotNull(saved);
        Assert.NotEmpty(saved.Embedding);
        await EmbeddingService.Received(1)
            .EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private record NotesRequest(
        string? Worldbuilding, string? Magic, string? Characters,
        string? Themes, string? PlotStructure, string? WhatILiked);

    private record WorkRequest(
        string Title, WorkType Type, string[] Genres, WorkStatus Status,
        object? Progress, NotesRequest Notes, string[] Tags);

    [Fact(DisplayName = "Returns 422 when Title is empty")]
    public async Task Should_Return422_When_TitleIsEmpty()
    {
        var request = MinimalRequest() with { Title = "" };

        var response = await Client.PostAsJsonAsync("/logbook/works", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "Returns 422 when all Notes fields are null")]
    public async Task Should_Return422_When_AllNotesAreNull()
    {
        var request = MinimalRequest() with { Notes = new(null, null, null, null, null, null) };

        var response = await Client.PostAsJsonAsync("/logbook/works", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private static WorkRequest MinimalRequest(string title = "Test Work") =>
        new(
            Title: title,
            Type: WorkType.Book,
            Genres: [],
            Status: WorkStatus.InProgress,
            Progress: null,
            Notes: new(Worldbuilding: "Some world", null, null, null, null, null),
            Tags: []);
}
