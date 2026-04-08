using LoreForge.Api.Extensions;
using LoreForge.Core.Entities;
using LoreForge.Core.Errors;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Api.Features.Logbook;

public record AddJournalEntryRequest(
    Guid? WorkId,
    string? ProgressSnapshot,
    JournalSource Source,
    string RawContent,
    string? FileRef
);

public class AddJournalEntryHandler(LoreForgeDbContext db, IEmbeddingService embedding) : IEndpoint
{
    public async Task<Result<Guid>> HandleAsync(AddJournalEntryRequest request, CancellationToken ct)
    {
        if (request.WorkId is not null)
        {
            var workExists = await db.Works.AnyAsync(w => w.Id == request.WorkId, ct);
            if (!workExists)
                return Result.Failure<Guid>(WorkErrors.NotFound(request.WorkId.Value));
        }

        var vector = await embedding.EmbedAsync(request.RawContent, ct);

        var result = JournalEntry.Create(request.WorkId, request.ProgressSnapshot, request.Source, request.RawContent, request.FileRef, vector);
        if (!result.IsSuccess)
            return Result.Failure<Guid>(result.Error!);

        db.JournalEntries.Add(result.Value);
        await db.SaveChangesAsync(ct);

        return Result.Success(result.Value.Id);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/logbook/journal-entries", async (
            [FromBody] AddJournalEntryRequest request,
            [FromServices] AddJournalEntryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult(id => Results.Created($"/logbook/journal-entries/{id}", id));
        });
}
