using LoreForge.Api.Extensions;
using LoreForge.Contracts.Logbook;
using LoreForge.Core.Errors;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoreForge.Api.Features.Logbook;

public class GetWorkByIdHandler(LoreForgeDbContext db) : IEndpoint
{
    public async Task<Result<WorkDetail>> HandleAsync(Guid id, CancellationToken ct)
    {
        var work = await db.Works
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new WorkDetail(
                w.Id,
                w.Title,
                w.Type,
                w.Genres,
                w.Status,
                w.Progress,
                new WorkNotesDto(
                    w.Notes.Worldbuilding,
                    w.Notes.Magic,
                    w.Notes.Characters,
                    w.Notes.Themes,
                    w.Notes.PlotStructure,
                    w.Notes.WhatILiked),
                w.Tags,
                w.CreatedAt))
            .FirstOrDefaultAsync(ct);

        if (work is null)
            return Result.Failure<WorkDetail>(WorkErrors.NotFound(id));

        return Result.Success(work);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/logbook/works/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] GetWorkByIdHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.ToHttpResult(work => Results.Ok(work));
        });
}
