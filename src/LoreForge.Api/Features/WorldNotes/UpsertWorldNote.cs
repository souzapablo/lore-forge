using LoreForge.Api.Extensions;
using LoreForge.Core.Entities;
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
        var existing = await db.WorldNotes
            .FirstOrDefaultAsync(n => n.Title == request.Title && n.Category == request.Category, ct);

        var vector = await embedding.EmbedAsync(request.Content, ct);

        if (existing is not null)
        {
            var updateResult = existing.Update(request.Content, vector);
            if (!updateResult.IsSuccess)
                return Result.Failure<Guid>(updateResult.Error!);

            await db.SaveChangesAsync(ct);
            return Result.Success(existing.Id);
        }

        var createResult = WorldNote.Create(request.Category, request.Title, request.Content, vector);
        if (!createResult.IsSuccess)
            return Result.Failure<Guid>(createResult.Error!);

        db.WorldNotes.Add(createResult.Value);
        await db.SaveChangesAsync(ct);

        return Result.Success(createResult.Value.Id);
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
}
