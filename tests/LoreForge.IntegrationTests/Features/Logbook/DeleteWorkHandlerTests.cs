using LoreForge.Core.Entities;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Logbook;

[Collection(PostgresCollection.Name)]
public class DeleteWorkHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 204 No Content when a work is deleted")]
    public async Task Should_Return204_When_WorkIsDeleted()
    {
        var id = await CreateWork();

        var response = await Client.DeleteAsync($"/logbook/works/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(DisplayName = "Removes the work from the database when deleted")]
    public async Task Should_RemoveWork_When_WorkIsDeleted()
    {
        var id = await CreateWork();

        await Client.DeleteAsync($"/logbook/works/{id}");

        var saved = await Context.Works.FindAsync(id);
        Assert.Null(saved);
    }

    [Fact(DisplayName = "Returns 404 Not Found when the work does not exist")]
    public async Task Should_Return404_When_WorkDoesNotExist()
    {
        var response = await Client.DeleteAsync($"/logbook/works/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
