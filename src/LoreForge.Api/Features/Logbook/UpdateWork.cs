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
        if (Validate(request) is { } error)
            return Result.Failure<bool>(error);

        var work = await db.Works.FirstOrDefaultAsync(w => w.Id == id, ct);

        if (work is null)
            return Result.Failure<bool>(WorkErrors.NotFound(id));

        work.Title = request.Title;
        work.Genres = request.Genres;
        work.Status = request.Status;
        work.Progress = request.Progress;
        work.Tags = request.Tags;
        work.Notes = new WorkNotes
        {
            Worldbuilding = request.Notes.Worldbuilding,
            Magic = request.Notes.Magic,
            Characters = request.Notes.Characters,
            Themes = request.Notes.Themes,
            PlotStructure = request.Notes.PlotStructure,
            WhatILiked = request.Notes.WhatILiked
        };
        work.Embedding = await embedding.EmbedAsync(work.Notes.ToEmbeddingText(work.Title), ct);

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

    private static Error? Validate(UpdateWorkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return WorkErrors.TitleEmpty;

        var hasNotes = request.Notes is
        {
            Worldbuilding: not null
        } or {
            Magic: not null
        } or {
            Characters: not null
        } or {
            Themes: not null
        } or {
            PlotStructure: not null
        } or {
            WhatILiked: not null
        };

        if (!hasNotes)
            return WorkErrors.NotesEmpty;

        return null;
    }
}
