using LoreForge.Api.Features.Logbook;
using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LoreForge.UnitTests.Features.Logbook;

public class AddWorkHandlerTests
{
    private sealed class TestDbContext(DbContextOptions<LoreForgeDbContext> options)
        : LoreForgeDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Work>(e =>
            {
                e.HasKey(x => x.Id);
                e.Ignore(x => x.Progress);
                e.OwnsOne(x => x.Notes);
            });
            modelBuilder.Entity<JournalEntry>(e => e.HasKey(x => x.Id));
            modelBuilder.Entity<WorldNote>(e => e.HasKey(x => x.Id));
        }
    }

    private static LoreForgeDbContext CreateDb() =>
        new TestDbContext(new DbContextOptionsBuilder<LoreForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IEmbeddingService MockEmbedding() =>
        Substitute.For<IEmbeddingService>();

    [Fact(DisplayName = "Uses only the title as embedding text when no notes are provided")]
    public async Task Should_EmbedTitleOnly_When_NoNotesProvided()
    {
        var embedding = MockEmbedding();
        var handler = new AddWorkHandler(CreateDb(), embedding);

        await handler.HandleAsync(MinimalRequest("Only Title"), CancellationToken.None);

        await embedding.Received(1).EmbedAsync(
            Arg.Is<string>(text => text == "Only Title"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Includes non-null notes fields in the embedding text")]
    public async Task Should_IncludeNotesInEmbeddingText_When_NotesAreProvided()
    {
        var embedding = MockEmbedding();
        var handler = new AddWorkHandler(CreateDb(), embedding);
        var request = MinimalRequest() with
        {
            Notes = new AddWorkNotesRequest(
                Worldbuilding: "The Shattered Realm",
                Magic: null,
                Characters: null,
                Themes: "Sacrifice",
                PlotStructure: null,
                WhatILiked: null)
        };

        await handler.HandleAsync(request, CancellationToken.None);

        await embedding.Received(1).EmbedAsync(
            Arg.Is<string>(text =>
                text.Contains("Worldbuilding: The Shattered Realm") &&
                text.Contains("Themes: Sacrifice")),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Excludes null notes fields from the embedding text")]
    public async Task Should_ExcludeNotesFromEmbeddingText_When_NotesAreNull()
    {
        var embedding = MockEmbedding();
        var handler = new AddWorkHandler(CreateDb(), embedding);
        var request = MinimalRequest() with
        {
            Notes = new AddWorkNotesRequest(
                Worldbuilding: null,
                Magic: "Some magic",
                Characters: null,
                Themes: null,
                PlotStructure: null,
                WhatILiked: null)
        };

        await handler.HandleAsync(request, CancellationToken.None);

        await embedding.Received(1).EmbedAsync(
            Arg.Is<string>(text =>
                !text.Contains("Worldbuilding:") &&
                !text.Contains("Themes:") &&
                text.Contains("Magic: Some magic")),
            Arg.Any<CancellationToken>());
    }

    private static AddWorkRequest MinimalRequest(string title = "Test Work") =>
        new(
            Title: title,
            Type: WorkType.Book,
            Genres: [],
            Status: WorkStatus.InProgress,
            Progress: null,
            Notes: new AddWorkNotesRequest(null, null, null, null, null, null),
            Tags: []);
}
