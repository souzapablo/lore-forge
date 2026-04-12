using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Bedrock.AgentTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text.Json;

namespace LoreForge.UnitTests.Infrastructure.Bedrock.AgentTools;

public class FindThematicConnectionsToolTests
{
    private static readonly float[] QueryEmbedding = [0.1f, 0.2f];

    [Fact(DisplayName = "Returns no results message when neither collection returns results")]
    public async Task Should_ReturnNoResultsMessage_When_NeitherCollectionReturnsResults()
    {
        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("sacrifice"), CancellationToken.None);

        Assert.Equal("No thematic connections found in the logbook.", result);
    }

    [Fact(DisplayName = "Includes thematic patterns section with title, type, status and themes when works are found")]
    public async Task Should_IncludeThematicPatternsSection_When_OnlyWorksFound()
    {
        var work = Work.Create(
            "Breaking Bad",
            WorkType.Series,
            ["drama", "crime"],
            WorkStatus.Completed,
            null,
            new WorkNotes { Themes = "Pride, hubris, and the corruption of good intentions" },
            [],
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([work]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("moral corruption"), CancellationToken.None);

        Assert.Contains("## Thematic patterns from your logbook", result);
        Assert.Contains("Breaking Bad", result);
        Assert.Contains("Series", result);
        Assert.Contains("Completed", result);
        Assert.Contains("Pride, hubris, and the corruption of good intentions", result);
        Assert.DoesNotContain("## Journal reflections on this theme", result);
    }

    [Fact(DisplayName = "Includes journal reflections section with date and content when entries are found")]
    public async Task Should_IncludeJournalReflectionsSection_When_OnlyEntriesFound()
    {
        var entry = JournalEntry.Create(
            null,
            null,
            JournalSource.PlainText,
            "Every story I love has a character who sacrifices everything for someone they barely know.",
            null,
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([entry]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("sacrifice"), CancellationToken.None);

        Assert.Contains("## Journal reflections on this theme", result);
        Assert.Contains(entry.CreatedAt.ToString("yyyy-MM-dd"), result);
        Assert.Contains("Every story I love has a character who sacrifices everything", result);
        Assert.DoesNotContain("## Thematic patterns from your logbook", result);
    }

    [Fact(DisplayName = "Includes both sections when works and entries are found")]
    public async Task Should_IncludeBothSections_When_BothCollectionsReturnResults()
    {
        var work = Work.Create(
            "His Dark Materials",
            WorkType.Book,
            ["fantasy"],
            WorkStatus.Completed,
            null,
            new WorkNotes { Themes = "Loss of innocence, free will vs. authority" },
            [],
            []).Value;

        var entry = JournalEntry.Create(
            null,
            null,
            JournalSource.PlainText,
            "Coming-of-age stories always hit hardest when the world itself is the thing that grows up.",
            null,
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorksAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([work]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([entry]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("loss of innocence"), CancellationToken.None);

        Assert.Contains("## Thematic patterns from your logbook", result);
        Assert.Contains("His Dark Materials", result);
        Assert.Contains("Loss of innocence, free will vs. authority", result);
        Assert.Contains("## Journal reflections on this theme", result);
        Assert.Contains("Coming-of-age stories always hit hardest", result);
    }

    [Fact(DisplayName = "Embeds the theme input before searching")]
    public async Task Should_EmbedTheThemeInput_When_Executed()
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
        await tool.ExecuteAsync(InputWith("redemption through suffering"), CancellationToken.None);

        await embeddingService.Received(1)
            .EmbedAsync("redemption through suffering", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Returns null tool guidance when no path is configured")]
    public async Task Should_ReturnNullToolGuidance_When_GuidancePathNotConfigured()
    {
        var tool = CreateTool();

        Assert.Null(tool.ToolGuidance);

        await Task.CompletedTask;
    }

    [Fact(DisplayName = "Returns null tool guidance when configured file does not exist")]
    public async Task Should_ReturnNullToolGuidance_When_GuidanceFileDoesNotExist()
    {
        var config = BuildConfig("assets/prompts/tools/nonexistent.txt");
        var tool = CreateTool(config: config);

        Assert.Null(tool.ToolGuidance);

        await Task.CompletedTask;
    }

    [Fact(DisplayName = "Returns file contents as tool guidance when configured file exists")]
    public async Task Should_ReturnFileContents_When_GuidanceFileExists()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "  Always surface the deepest theme first.  ");
            var config = BuildConfig(path);
            var tool = CreateTool(config: config);

            Assert.Equal("Always surface the deepest theme first.", tool.ToolGuidance);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static FindThematicConnectionsTool CreateTool(
        IEmbeddingService? embedding = null,
        IVectorStore? vectorStore = null,
        IConfiguration? config = null)
    {
        var embeddingService = embedding ?? Substitute.For<IEmbeddingService>();
        embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(QueryEmbedding);

        return new FindThematicConnectionsTool(
            embeddingService,
            vectorStore ?? Substitute.For<IVectorStore>(),
            config ?? new ConfigurationBuilder().Build(),
            NullLogger<FindThematicConnectionsTool>.Instance);
    }

    private static IConfiguration BuildConfig(string guidancePath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bedrock:ToolGuidancePaths:find_thematic_connections"] = guidancePath
            })
            .Build();

    private static JsonElement InputWith(string theme) =>
        JsonDocument.Parse($"{{\"theme\":\"{theme}\"}}").RootElement;
}
