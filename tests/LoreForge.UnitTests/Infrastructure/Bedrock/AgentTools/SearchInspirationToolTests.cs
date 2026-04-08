using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Bedrock.AgentTools;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text.Json;

namespace LoreForge.UnitTests.Infrastructure.Bedrock.AgentTools;

public class SearchInspirationToolTests
{
    private static readonly float[] QueryEmbedding = [0.1f, 0.2f];

    private static SearchInspirationTool CreateTool(
        IEmbeddingService? embedding = null,
        IVectorStore? vectorStore = null)
    {
        var embeddingService = embedding ?? Substitute.For<IEmbeddingService>();
        embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(QueryEmbedding);

        return new SearchInspirationTool(
            embeddingService,
            vectorStore ?? Substitute.For<IVectorStore>(),
            NullLogger<SearchInspirationTool>.Instance);
    }

    private static JsonElement InputWith(string query) =>
        JsonDocument.Parse($"{{\"query\":\"{query}\"}}").RootElement;

    [Fact(DisplayName = "Returns no results message when logbook is empty")]
    public async Task Should_ReturnNoResultsMessage_When_LogbookIsEmpty()
    {
        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("dragons"), CancellationToken.None);

        Assert.Equal("No relevant inspiration found in the logbook.", result);
    }

    [Fact(DisplayName = "Includes work title, type, genres and notes in output when works are found")]
    public async Task Should_IncludeWorkDetails_When_WorksFound()
    {
        var work = Work.Create(
            "Elden Ring",
            WorkType.Game,
            ["action", "rpg"],
            WorkStatus.Completed,
            null,
            new WorkNotes { Themes = "Death and renewal" },
            [],
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([work]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("open world game"), CancellationToken.None);

        Assert.Contains("Elden Ring", result);
        Assert.Contains("Game", result);
        Assert.Contains("Completed", result);
        Assert.Contains("action, rpg", result);
        Assert.Contains("Death and renewal", result);
    }

    [Fact(DisplayName = "Includes journal entry content and date in output when entries are found")]
    public async Task Should_IncludeJournalEntryContent_When_EntriesFound()
    {
        var entry = JournalEntry.Create(
            null,
            null,
            JournalSource.PlainText,
            "The magic system felt elegant and intuitive.",
            null,
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([entry]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("magic systems"), CancellationToken.None);

        Assert.Contains("The magic system felt elegant and intuitive.", result);
        Assert.Contains(entry.CreatedAt.ToString("yyyy-MM-dd"), result);
    }

    [Fact(DisplayName = "Embeds the query before searching")]
    public async Task Should_EmbedTheQuery_When_Executed()
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
        await tool.ExecuteAsync(InputWith("dragons and magic"), CancellationToken.None);

        await embeddingService.Received(1)
            .EmbedAsync("dragons and magic", Arg.Any<CancellationToken>());
    }
}
