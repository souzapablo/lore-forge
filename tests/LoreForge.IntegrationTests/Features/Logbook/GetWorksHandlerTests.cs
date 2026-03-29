using LoreForge.Api.Features.Logbook;
using LoreForge.Core.Entities;
using LoreForge.Core.Primitives;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Logbook;

[Collection(PostgresCollection.Name)]
public class GetWorksHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 200 OK with an empty list when no works exist")]
    public async Task Should_ReturnEmptyList_When_NoWorksExist()
    {
        var response = await Client.GetAsync("/logbook/works");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact(DisplayName = "Returns all works when no filters are applied")]
    public async Task Should_ReturnAllWorks_When_NoFiltersApplied()
    {
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Elden Ring", WorkType.Game, WorkStatus.Completed));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Dune", WorkType.Book, WorkStatus.InProgress));

        var response = await Client.GetAsync("/logbook/works");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, w => w.Title == "Elden Ring");
        Assert.Contains(result.Items, w => w.Title == "Dune");
    }

    [Fact(DisplayName = "Returns correct summary fields for a work")]
    public async Task Should_ReturnCorrectFields_When_WorkIsReturned()
    {
        var request = new
        {
            Title = "Elden Ring",
            Type = WorkType.Game,
            Genres = new[] { "rpg" },
            Status = WorkStatus.Completed,
            Progress = (object?)null,
            Notes = new { Worldbuilding = "Some world", Magic = (string?)null, Characters = (string?)null, Themes = (string?)null, PlotStructure = (string?)null, WhatILiked = (string?)null },
            Tags = new[] { "fromsoft" }
        };

        await Client.PostAsJsonAsync("/logbook/works", request);

        var response = await Client.GetAsync("/logbook/works");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();
        var work = Assert.Single(result!.Items);

        Assert.Equal("Elden Ring", work.Title);
        Assert.Equal(WorkType.Game, work.Type);
        Assert.Equal(["rpg"], work.Genres);
        Assert.Equal(WorkStatus.Completed, work.Status);
        Assert.Equal(["fromsoft"], work.Tags);
        Assert.NotEqual(Guid.Empty, work.Id);
    }

    [Fact(DisplayName = "Returns correct pagination metadata")]
    public async Task Should_ReturnCorrectPaginationMetadata_When_WorksArePaged()
    {
        for (var i = 1; i <= 15; i++)
            await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest($"Work {i}"));

        var response = await Client.GetAsync("/logbook/works?page=1&pageSize=10");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact(DisplayName = "Returns second page of results")]
    public async Task Should_ReturnSecondPage_When_PageTwoIsRequested()
    {
        for (var i = 1; i <= 15; i++)
            await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest($"Work {i}"));

        var response = await Client.GetAsync("/logbook/works?page=2&pageSize=10");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact(DisplayName = "Filters works by a single type")]
    public async Task Should_ReturnOnlyMatchingWorks_When_FilteredByType()
    {
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Elden Ring", WorkType.Game, WorkStatus.Completed));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Dune", WorkType.Book, WorkStatus.Completed));

        var response = await Client.GetAsync("/logbook/works?types=0");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.All(result.Items, w => Assert.Equal(WorkType.Game, w.Type));
    }

    [Fact(DisplayName = "Filters works by multiple types")]
    public async Task Should_ReturnMatchingWorks_When_FilteredByMultipleTypes()
    {
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Elden Ring", WorkType.Game, WorkStatus.Completed));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Dune", WorkType.Book, WorkStatus.Completed));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Inception", WorkType.Movie, WorkStatus.Completed));

        var response = await Client.GetAsync("/logbook/works?types=0&types=1");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, w => w.Title == "Elden Ring");
        Assert.Contains(result.Items, w => w.Title == "Dune");
        Assert.DoesNotContain(result.Items, w => w.Title == "Inception");
    }

    [Fact(DisplayName = "Filters works by multiple statuses")]
    public async Task Should_ReturnMatchingWorks_When_FilteredByMultipleStatuses()
    {
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Elden Ring", WorkType.Game, WorkStatus.Completed));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Dune", WorkType.Book, WorkStatus.InProgress));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Dark Souls", WorkType.Game, WorkStatus.Dropped));

        var response = await Client.GetAsync("/logbook/works?statuses=0&statuses=1");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, w => w.Title == "Elden Ring");
        Assert.Contains(result.Items, w => w.Title == "Dune");
        Assert.DoesNotContain(result.Items, w => w.Title == "Dark Souls");
    }

    [Fact(DisplayName = "Filters works by tags")]
    public async Task Should_ReturnOnlyMatchingWorks_When_FilteredByTags()
    {
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Elden Ring", WorkType.Game, WorkStatus.Completed, ["fromsoft", "souls-like"]));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Dune", WorkType.Book, WorkStatus.Completed, ["sci-fi"]));

        var response = await Client.GetAsync("/logbook/works?tags=fromsoft");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, w => w.Title == "Elden Ring");
        Assert.DoesNotContain(result.Items, w => w.Title == "Dune");
    }

    [Fact(DisplayName = "Filters works by a single genre")]
    public async Task Should_ReturnOnlyMatchingWorks_When_FilteredBySingleGenre()
    {
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Elden Ring", genres: ["rpg", "action"]));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Dune", genres: ["sci-fi"]));

        var response = await Client.GetAsync("/logbook/works?genres=rpg");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, w => w.Title == "Elden Ring");
        Assert.DoesNotContain(result.Items, w => w.Title == "Dune");
    }

    [Fact(DisplayName = "Filters works by multiple genres")]
    public async Task Should_ReturnMatchingWorks_When_FilteredByMultipleGenres()
    {
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Elden Ring", genres: ["rpg", "action"]));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Dune", genres: ["sci-fi", "epic"]));
        await Client.PostAsJsonAsync("/logbook/works", MinimalWorkRequest("Inception", genres: ["thriller"]));

        var response = await Client.GetAsync("/logbook/works?genres=rpg&genres=sci-fi");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkSummary>>();

        Assert.NotNull(result);
        Assert.Contains(result.Items, w => w.Title == "Elden Ring");
        Assert.Contains(result.Items, w => w.Title == "Dune");
        Assert.DoesNotContain(result.Items, w => w.Title == "Inception");
    }

    private static object MinimalWorkRequest(
        string title,
        WorkType type = WorkType.Book,
        WorkStatus status = WorkStatus.InProgress,
        string[]? tags = null,
        string[]? genres = null) => new
    {
        Title = title,
        Type = type,
        Genres = genres ?? Array.Empty<string>(),
        Status = status,
        Progress = (object?)null,
        Notes = new { Worldbuilding = "Some world", Magic = (string?)null, Characters = (string?)null, Themes = (string?)null, PlotStructure = (string?)null, WhatILiked = (string?)null },
        Tags = tags ?? Array.Empty<string>()
    };
}
