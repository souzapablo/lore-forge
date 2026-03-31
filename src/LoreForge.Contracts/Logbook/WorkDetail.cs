using System.Text.Json;
using LoreForge.Core.Entities;

namespace LoreForge.Contracts.Logbook;

public record WorkDetail(
    Guid Id,
    string Title,
    WorkType Type,
    string[] Genres,
    WorkStatus Status,
    JsonDocument? Progress,
    WorkNotesDto Notes,
    string[] Tags,
    DateTime CreatedAt
);

public record WorkNotesDto(
    string? Worldbuilding,
    string? Magic,
    string? Characters,
    string? Themes,
    string? PlotStructure,
    string? WhatILiked
);
