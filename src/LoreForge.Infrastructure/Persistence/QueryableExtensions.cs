using LoreForge.Core.Entities;
using LoreForge.Core.Filtering;
using LoreForge.Core.Primitives;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Infrastructure.Persistence;

public static class QueryableExtensions
{
    public static IQueryable<Work> ApplyFilter(this IQueryable<Work> query, WorkFilter filter)
    {
        if (filter.Types is { Length: > 0 })
            query = query.Where(w => filter.Types.Contains(w.Type));

        if (filter.Statuses is { Length: > 0 })
            query = query.Where(w => filter.Statuses.Contains(w.Status));

        if (filter.Tags is { Length: > 0 })
            query = query.Where(w => w.Tags.Any(t => filter.Tags.Contains(t)));

        if (filter.Genres is { Length: > 0 })
            query = query.Where(w => w.Genres.Any(g => filter.Genres.Contains(g)));

        return query;
    }

    public static IQueryable<JournalEntry> ApplyFilter(this IQueryable<JournalEntry> query, JournalEntryFilter filter)
    {
        if (filter.WorkId is not null)
            query = query.Where(e => e.WorkId == filter.WorkId);

        if (filter.Sources is { Length: > 0 })
            query = query.Where(e => filter.Sources.Contains(e.Source));

        if (filter.From is not null)
            query = query.Where(e => e.CreatedAt >= filter.From);

        if (filter.To is not null)
            query = query.Where(e => e.CreatedAt <= filter.To);

        return query;
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationParams pagination,
        CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, totalCount, pagination.Page, pagination.PageSize);
    }
}
