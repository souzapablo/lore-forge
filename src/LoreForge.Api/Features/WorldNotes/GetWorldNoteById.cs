using LoreForge.Api.Extensions;
using LoreForge.Contracts.WorldNotes;
using LoreForge.Core.Errors;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Api.Features.WorldNotes;

public class GetWorldNoteByIdHandler(LoreForgeDbContext db) : IEndpoint
{
    public async Task<Result<WorldNoteSummary>> HandleAsync(Guid id, CancellationToken ct)
    {
        var note = await db.WorldNotes
            .AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => new WorldNoteSummary(n.Id, n.Category, n.Title, n.Content, n.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        if (note is null)
            return Result.Failure<WorldNoteSummary>(WorldNoteErrors.NotFound(id));

        return Result.Success(note);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/world-notes/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] GetWorldNoteByIdHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.ToHttpResult(note => Results.Ok(note));
        });
}
