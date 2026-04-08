using LoreForge.Api.Extensions;
using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LoreForge.Api.Features.Logbook;

public record AddWorkRequest(
    string Title,
    WorkType Type,
    string[] Genres,
    WorkStatus Status,
    JsonDocument? Progress,
    AddWorkNotesRequest Notes,
    string[] Tags
);

public record AddWorkNotesRequest(
    string? Worldbuilding,
    string? Magic,
    string? Characters,
    string? Themes,
    string? PlotStructure,
    string? WhatILiked
);

public class AddWorkHandler(LoreForgeDbContext db, IEmbeddingService embedding) : IEndpoint
{
    public async Task<Result<Guid>> HandleAsync(AddWorkRequest request, CancellationToken ct)
    {
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

        var result = Work.Create(request.Title, request.Type, request.Genres, request.Status, request.Progress, notes, request.Tags, vector);
        if (!result.IsSuccess)
            return Result.Failure<Guid>(result.Error!);

        db.Works.Add(result.Value);
        await db.SaveChangesAsync(ct);

        return Result.Success(result.Value.Id);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/logbook/works", async (
            [FromBody] AddWorkRequest request,
            [FromServices] AddWorkHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult(id => Results.Created($"/logbook/works/{id}", id));
        });
}
