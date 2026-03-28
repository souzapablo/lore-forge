using LoreForge.Api.Extensions;
using LoreForge.Core.Entities;
using LoreForge.Core.Filtering;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Api.Features.Logbook;

public record JournalEntrySummary(
    Guid Id,
    Guid? WorkId,
    string? ProgressSnapshot,
    JournalSource Source,
    string RawContent,
    DateTime CreatedAt
);

public class GetJournalEntriesHandler(LoreForgeDbContext db) : IEndpoint
{
    public async Task<Result<PagedResult<JournalEntrySummary>>> HandleAsync(
        JournalEntryFilter filter,
        PaginationParams pagination,
        CancellationToken ct)
    {
        var entries = await db.JournalEntries
            .AsNoTracking()
            .ApplyFilter(filter)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new JournalEntrySummary(e.Id, e.WorkId, e.ProgressSnapshot, e.Source, e.RawContent, e.CreatedAt))
            .ToPagedResultAsync(pagination, ct);

        return Result.Success(entries);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/logbook/journal-entries", async (
            [FromQuery] Guid? workId,
            [FromQuery] JournalSource[]? sources,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromServices] GetJournalEntriesHandler handler,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var filter = new JournalEntryFilter(workId, sources, from, to);
            var pagination = new PaginationParams(page, pageSize);
            var result = await handler.HandleAsync(filter, pagination, ct);
            return Results.Ok(result.Value);
        });
}
