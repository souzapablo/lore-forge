using LoreForge.Api.Extensions;
using LoreForge.Core.Entities;
using LoreForge.Core.Errors;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LoreForge.Api.Features.Logbook;

public record UpdateWorkRequest(
    string Title,
    string[] Genres,
    WorkStatus Status,
    JsonDocument? Progress,
    UpdateWorkNotesRequest Notes,
    string[] Tags
);

public record UpdateWorkNotesRequest(
    string? Worldbuilding,
    string? Magic,
    string? Characters,
    string? Themes,
    string? PlotStructure,
    string? WhatILiked
);

public class UpdateWorkHandler(LoreForgeDbContext db, IEmbeddingService embedding) : IEndpoint
{
    public async Task<Result<bool>> HandleAsync(Guid id, UpdateWorkRequest request, CancellationToken ct)
    {
        var work = await db.Works.FirstOrDefaultAsync(w => w.Id == id, ct);

        if (work is null)
            return Result.Failure<bool>(WorkErrors.NotFound(id));

        var notes = new WorkNotes
        {
            Worldbuilding = request.Notes.Worldbuilding,
            Magic = request.Notes.Magic,
            Characters = request.Notes.Characters,
            Themes = request.Notes.Themes,
            PlotStructure = request.Notes.PlotStructure,
            WhatILiked = request.Notes.WhatILiked
        };

        var vector = await embedding.EmbedAsync(notes.ToEmbeddingText(request.Title), ct);

        var result = work.Update(request.Title, request.Genres, request.Status, request.Progress, notes, request.Tags, vector);
        if (!result.IsSuccess)
            return Result.Failure<bool>(result.Error!);

        await db.SaveChangesAsync(ct);

        return Result.Success(true);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/logbook/works/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateWorkRequest request,
            [FromServices] UpdateWorkHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });
}
