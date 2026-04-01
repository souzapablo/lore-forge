using LoreForge.Api.Extensions;
using LoreForge.Core.Entities;
using LoreForge.Core.Errors;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Api.Features.WorldNotes;

public record UpsertWorldNoteRequest(
    WorldNoteCategory Category,
    string Title,
    string Content
);

public class UpsertWorldNoteHandler(LoreForgeDbContext db, IEmbeddingService embedding) : IEndpoint
{
    public async Task<Result<Guid>> HandleAsync(UpsertWorldNoteRequest request, CancellationToken ct)
    {
        if (Validate(request) is { } error)
            return Result.Failure<Guid>(error);

        var existing = await db.WorldNotes
            .FirstOrDefaultAsync(n => n.Title == request.Title && n.Category == request.Category, ct);

        var vector = await embedding.EmbedAsync(request.Content, ct);

        if (existing is not null)
        {
            existing.Content = request.Content;
            existing.Embedding = vector;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result.Success(existing.Id);
        }

        var note = new WorldNote
        {
            Id = Guid.NewGuid(),
            Category = request.Category,
            Title = request.Title,
            Content = request.Content,
            Embedding = vector,
            UpdatedAt = DateTime.UtcNow
        };

        db.WorldNotes.Add(note);
        await db.SaveChangesAsync(ct);

        return Result.Success(note.Id);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/world-notes", async (
            [FromBody] UpsertWorldNoteRequest request,
            [FromServices] UpsertWorldNoteHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult(id => Results.Ok(id));
        });

    private static Error? Validate(UpsertWorldNoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return WorldNoteErrors.TitleEmpty;

        if (string.IsNullOrWhiteSpace(request.Content))
            return WorldNoteErrors.ContentEmpty;

        return null;
    }
}
