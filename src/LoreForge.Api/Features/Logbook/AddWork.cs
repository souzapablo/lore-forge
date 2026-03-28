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

public class AddWorkHandler(LoreForgeDbContext db, IEmbeddingService embedding)
{
    public async Task<Result<Guid>> HandleAsync(AddWorkRequest request, CancellationToken ct)
    {
        var embeddingText = BuildEmbeddingText(request);
        var vector = await embedding.EmbedAsync(embeddingText, ct);

        var work = new Work
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Type = request.Type,
            Genres = request.Genres,
            Status = request.Status,
            Progress = request.Progress,
            Notes = new WorkNotes
            {
                Worldbuilding = request.Notes.Worldbuilding,
                Magic = request.Notes.Magic,
                Characters = request.Notes.Characters,
                Themes = request.Notes.Themes,
                PlotStructure = request.Notes.PlotStructure,
                WhatILiked = request.Notes.WhatILiked
            },
            Tags = request.Tags,
            Embedding = vector
        };

        db.Works.Add(work);
        await db.SaveChangesAsync(ct);

        return Result.Success(work.Id);
    }

    private static string BuildEmbeddingText(AddWorkRequest r)
    {
        var parts = new List<string> { r.Title };
        if (r.Notes.Worldbuilding is not null) parts.Add($"Worldbuilding: {r.Notes.Worldbuilding}");
        if (r.Notes.Magic is not null) parts.Add($"Magic: {r.Notes.Magic}");
        if (r.Notes.Characters is not null) parts.Add($"Characters: {r.Notes.Characters}");
        if (r.Notes.Themes is not null) parts.Add($"Themes: {r.Notes.Themes}");
        if (r.Notes.PlotStructure is not null) parts.Add($"Plot structure: {r.Notes.PlotStructure}");
        if (r.Notes.WhatILiked is not null) parts.Add($"What I liked: {r.Notes.WhatILiked}");
        return string.Join("\n", parts);
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
