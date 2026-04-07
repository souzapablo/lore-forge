using LoreForge.Api.Extensions;
using LoreForge.Core.Errors;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LoreForge.Api.Features.WorldNotes;

public class DeleteWorldNoteHandler(LoreForgeDbContext db) : IEndpoint
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct)
    {
        var note = await db.WorldNotes.FindAsync([id], ct);
        if (note is null)
            return Result.Failure(WorldNoteErrors.NotFound(id));

        db.WorldNotes.Remove(note);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/world-notes/{id:guid}", async (
            Guid id,
            [FromServices] DeleteWorldNoteHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(result.Error);
        });
}
