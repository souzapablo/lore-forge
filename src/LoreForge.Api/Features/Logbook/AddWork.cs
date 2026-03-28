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

        var embeddedNotes = notes.ToEmbeddingText(request.Title);
        var vector = await embedding.EmbedAsync(embeddedNotes, ct);

        var work = new Work
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Type = request.Type,
            Genres = request.Genres,
            Status = request.Status,
            Progress = request.Progress,
            Notes = notes,
            Tags = request.Tags,
            Embedding = vector
        };

        db.Works.Add(work);
        await db.SaveChangesAsync(ct);

        return Result.Success(work.Id);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/logbook/works", async (
            [FromBody] AddWorkRequest request,
            [FromServices] AddWorkHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        });
}
