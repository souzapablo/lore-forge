using LoreForge.Api.Extensions;
using LoreForge.Contracts.Logbook;
using LoreForge.Core.Entities;
using LoreForge.Core.Filtering;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Api.Features.Logbook;

public class GetWorksHandler(LoreForgeDbContext db) : IEndpoint
{
    public async Task<Result<PagedResult<WorkSummary>>> HandleAsync(WorkFilter filter, PaginationParams pagination, CancellationToken ct)
    {
        var works = await db.Works
            .AsNoTracking()
            .ApplyFilter(filter)
            .Select(w => new WorkSummary(w.Id, w.Title, w.Type, w.Genres, w.Status, w.Tags, w.CreatedAt))
            .ToPagedResultAsync(pagination, ct);

        return Result.Success(works);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/logbook/works", async (
            [FromQuery] WorkType[]? types,
            [FromQuery] WorkStatus[]? statuses,
            [FromQuery] string[]? genres,
            [FromQuery] string[]? tags,
            [FromServices] GetWorksHandler handler,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var filter = new WorkFilter(types, statuses, genres, tags);
            var pagination = new PaginationParams(page, pageSize);
            var result = await handler.HandleAsync(filter, pagination, ct);
            return Results.Ok(result.Value);
        });
}
