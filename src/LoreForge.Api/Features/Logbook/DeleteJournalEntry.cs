using LoreForge.Api.Extensions;
using LoreForge.Core.Errors;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LoreForge.Api.Features.Logbook;

public class DeleteJournalEntryHandler(LoreForgeDbContext db) : IEndpoint
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct)
    {
        var entry = await db.JournalEntries.FindAsync([id], ct);
        if (entry is null)
            return Result.Failure(JournalEntryErrors.NotFound(id));

        db.JournalEntries.Remove(entry);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/logbook/journal-entries/{id:guid}", async (
            Guid id,
            [FromServices] DeleteJournalEntryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(result.Error);
        });
}
