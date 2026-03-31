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
        if (Validate(request) is { } error)
            return Result.Failure<Guid>(error);

        if (request.WorkId is not null)
        {
            var workExists = await db.Works.AnyAsync(w => w.Id == request.WorkId, ct);
            if (!workExists)
                return Result.Failure<Guid>(WorkErrors.NotFound(request.WorkId.Value));
        }

        var vector = await embedding.EmbedAsync(request.RawContent, ct);

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            WorkId = request.WorkId,
            ProgressSnapshot = request.ProgressSnapshot,
            Source = request.Source,
            RawContent = request.RawContent,
            FileRef = request.FileRef,
            Embedding = vector
        };

        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return Result.Success(entry.Id);
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
    
    private static Error? Validate(AddJournalEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawContent))
            return JournalEntryErrors.ContentEmpty;

        if (request.Source == JournalSource.File && string.IsNullOrWhiteSpace(request.FileRef))
            return JournalEntryErrors.FileRefRequired;

        return null;
    }
}
