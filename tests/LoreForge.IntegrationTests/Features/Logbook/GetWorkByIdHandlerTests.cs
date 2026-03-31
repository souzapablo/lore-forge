using LoreForge.Contracts.Logbook;
using LoreForge.Core.Entities;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Logbook;

[Collection(PostgresCollection.Name)]
public class GetWorkByIdHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 404 when work does not exist")]
    public async Task Should_Return404_When_WorkDoesNotExist()
    {
        var response = await Client.GetAsync($"/logbook/works/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "Returns 200 with full work detail when work exists")]
    public async Task Should_ReturnWorkDetail_When_WorkExists()
    {
        var createResponse = await Client.PostAsJsonAsync("/logbook/works", FullWorkRequest());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await Client.GetAsync($"/logbook/works/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var work = await response.Content.ReadFromJsonAsync<WorkDetail>();
        Assert.NotNull(work);
        Assert.Equal(id, work.Id);
        Assert.Equal("Elden Ring", work.Title);
        Assert.Equal(WorkType.Game, work.Type);
        Assert.Equal(WorkStatus.Completed, work.Status);
        Assert.Equal(["rpg", "action"], work.Genres);
        Assert.Equal(["fromsoft"], work.Tags);
    }

    [Fact(DisplayName = "Returns all notes fields in work detail")]
    public async Task Should_ReturnAllNotesFields_When_WorkHasNotes()
    {
        var createResponse = await Client.PostAsJsonAsync("/logbook/works", FullWorkRequest());
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await Client.GetAsync($"/logbook/works/{id}");
        var work = await response.Content.ReadFromJsonAsync<WorkDetail>();

        Assert.NotNull(work);
        Assert.Equal("A vast open world", work.Notes.Worldbuilding);
        Assert.Equal("Rune-based magic", work.Notes.Magic);
        Assert.Equal("Tarnished protagonist", work.Notes.Characters);
        Assert.Equal("Loss and perseverance", work.Notes.Themes);
        Assert.Equal("Non-linear exploration", work.Notes.PlotStructure);
        Assert.Equal("The boss design", work.Notes.WhatILiked);
    }

    [Fact(DisplayName = "Returns null for missing notes fields")]
    public async Task Should_ReturnNullNotesFields_When_OnlySomeNotesProvided()
    {
        var request = new
        {
            Title = "Dune",
            Type = WorkType.Book,
            Genres = Array.Empty<string>(),
            Status = WorkStatus.Completed,
            Progress = (object?)null,
            Notes = new { Worldbuilding = "Desert planet", Magic = (string?)null, Characters = (string?)null, Themes = (string?)null, PlotStructure = (string?)null, WhatILiked = (string?)null },
            Tags = Array.Empty<string>()
        };

        var createResponse = await Client.PostAsJsonAsync("/logbook/works", request);
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await Client.GetAsync($"/logbook/works/{id}");
        var work = await response.Content.ReadFromJsonAsync<WorkDetail>();

        Assert.NotNull(work);
        Assert.Equal("Desert planet", work.Notes.Worldbuilding);
        Assert.Null(work.Notes.Magic);
        Assert.Null(work.Notes.Characters);
        Assert.Null(work.Notes.Themes);
        Assert.Null(work.Notes.PlotStructure);
        Assert.Null(work.Notes.WhatILiked);
    }

    private static object FullWorkRequest() => new
    {
        Title = "Elden Ring",
        Type = WorkType.Game,
        Genres = new[] { "rpg", "action" },
        Status = WorkStatus.Completed,
        Progress = (object?)null,
        Notes = new
        {
            Worldbuilding = "A vast open world",
            Magic = "Rune-based magic",
            Characters = "Tarnished protagonist",
            Themes = "Loss and perseverance",
            PlotStructure = "Non-linear exploration",
            WhatILiked = "The boss design"
        },
        Tags = new[] { "fromsoft" }
    };
}
