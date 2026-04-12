using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Bedrock.AgentTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text.Json;

namespace LoreForge.UnitTests.Infrastructure.Bedrock.AgentTools;

public class SuggestCharacterArcToolTests
{
    private static readonly float[] QueryEmbedding = [0.1f, 0.2f];

    [Fact(DisplayName = "Returns no results message when logbook is empty")]
    public async Task Should_ReturnNoResultsMessage_WhenLogbookIsEmpty()
    {
        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("a reluctant hero"), CancellationToken.None);

        Assert.Equal("No relevant character arc references found in the logbook.", result);
    }

    [Fact(DisplayName = "Includes work title, type, status, characters and themes when works are found")]
    public async Task Should_IncludeWorkDetails_WhenWorksFound()
    {
        var work = Work.Create(
            "The Last of Us",
            WorkType.Game,
            ["action", "drama"],
            WorkStatus.Completed,
            null,
            new WorkNotes { Characters = "Joel and Ellie — surrogate father-daughter bond", Themes = "Love, loss, and sacrifice" },
            [],
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([work]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("a protective mentor figure"), CancellationToken.None);

        Assert.Contains("The Last of Us", result);
        Assert.Contains("Game", result);
        Assert.Contains("Completed", result);
        Assert.Contains("Joel and Ellie", result);
        Assert.Contains("Love, loss, and sacrifice", result);
        Assert.Contains("## Character arcs from your logbook", result);
    }

    [Fact(DisplayName = "Omits characters and themes lines when work notes are null")]
    public async Task Should_OmitNullNoteFields_WhenWorksFound()
    {
        var work = Work.Create(
            "Dune",
            WorkType.Book,
            [],
            WorkStatus.Completed,
            null,
            new WorkNotes { Worldbuilding = "Vast desert planet with political intrigue" },
            [],
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([work]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("a reluctant chosen one"), CancellationToken.None);

        Assert.Contains("Dune", result);
        Assert.DoesNotContain("Characters:", result);
        Assert.DoesNotContain("Themes:", result);
    }

    [Fact(DisplayName = "Includes journal entry content and date when entries are found")]
    public async Task Should_IncludeJournalEntryContent_WhenEntriesFound()
    {
        var entry = JournalEntry.Create(
            null,
            null,
            JournalSource.PlainText,
            "The way Walter White rationalizes each step of his transformation is chilling.",
            null,
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([entry]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("a villain origin story"), CancellationToken.None);

        Assert.Contains("Walter White", result);
        Assert.Contains(entry.CreatedAt.ToString("yyyy-MM-dd"), result);
        Assert.Contains("## Journal observations on characters", result);
    }

    [Fact(DisplayName = "Embeds the character description before searching")]
    public async Task Should_EmbedTheCharacterDescription_WhenExecuted()
    {
        var embeddingService = Substitute.For<IEmbeddingService>();
        embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(QueryEmbedding);

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(embedding: embeddingService, vectorStore: vectorStore);
        await tool.ExecuteAsync(InputWith("a lone wanderer seeking redemption"), CancellationToken.None);

        await embeddingService.Received(1)
            .EmbedAsync("a lone wanderer seeking redemption", Arg.Any<CancellationToken>());
    }

    private static SuggestCharacterArcTool CreateTool(
        IEmbeddingService? embedding = null,
        IVectorStore? vectorStore = null)
    {
        var embeddingService = embedding ?? Substitute.For<IEmbeddingService>();
        embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(QueryEmbedding);

        return new SuggestCharacterArcTool(
            embeddingService,
            vectorStore ?? Substitute.For<IVectorStore>(),
            Substitute.For<IConfiguration>(),
            NullLogger<SuggestCharacterArcTool>.Instance);
    }

    private static JsonElement InputWith(string characterDescription) =>
        JsonDocument.Parse($"{{\"character_description\":\"{characterDescription}\"}}").RootElement;
}
