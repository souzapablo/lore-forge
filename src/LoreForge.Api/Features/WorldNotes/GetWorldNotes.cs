using LoreForge.Api.Extensions;
using LoreForge.Contracts.WorldNotes;
using LoreForge.Core.Entities;
using LoreForge.Core.Filtering;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Api.Features.WorldNotes;

public class GetWorldNotesHandler(LoreForgeDbContext db) : IEndpoint
{
    public async Task<Result<PagedResult<WorldNoteSummary>>> HandleAsync(
        WorldNoteCategory? category,
        PaginationParams pagination,
        CancellationToken ct)
    {
        var notes = await db.WorldNotes
            .AsNoTracking()
            .Where(n => category == null || n.Category == category)
            .OrderBy(n => n.Title)
            .Select(n => new WorldNoteSummary(n.Id, n.Category, n.Title, n.Content, n.UpdatedAt))
            .ToPagedResultAsync(pagination, ct);

        return Result.Success(notes);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/world-notes", async (
            [FromServices] GetWorldNotesHandler handler,
            CancellationToken ct,
            [FromQuery] WorldNoteCategory? category = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var pagination = new PaginationParams(page, pageSize);
            var result = await handler.HandleAsync(category, pagination, ct);
            return Results.Ok(result.Value);
        });
}
