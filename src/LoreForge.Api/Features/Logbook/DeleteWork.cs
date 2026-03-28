using LoreForge.Api.Extensions;
using LoreForge.Core.Errors;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Api.Features.Logbook;

public class DeleteWorkHandler(LoreForgeDbContext db) : IEndpoint
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct)
    {
        var work = await db.Works.FindAsync([id], ct);
        if (work is null)
            return Result.Failure(WorkErrors.NotFound(id));

        db.Works.Remove(work);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/logbook/works/{id:guid}", async (
            Guid id,
            [FromServices] DeleteWorkHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(result.Error);
        });
}
