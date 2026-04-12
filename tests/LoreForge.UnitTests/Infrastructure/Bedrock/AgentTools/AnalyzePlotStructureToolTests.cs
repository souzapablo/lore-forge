using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Bedrock.AgentTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text.Json;

namespace LoreForge.UnitTests.Infrastructure.Bedrock.AgentTools;

public class AnalyzePlotStructureToolTests
{
    private static readonly float[] QueryEmbedding = [0.1f, 0.2f];

    [Fact(DisplayName = "Returns no results message when neither collection returns results")]
    public async Task Should_ReturnNoResultsMessage_When_NeitherCollectionReturnsResults()
    {
        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("how does the first act resolve?"), CancellationToken.None);

        Assert.Equal("No relevant plot structure references found.", result);
    }

    [Fact(DisplayName = "Includes world lore section with category, title, url and content when notes are found")]
    public async Task Should_IncludeWorldLoreSection_When_OnlyNotesFound()
    {
        var note = WorldNote.Create(WorldNoteCategory.Plot, "The Betrayal", "The king betrays his council in act two.", []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([note]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var config = BuildConfigWithWebBaseUrl("https://loreforge.example.com");
        var tool = CreateTool(vectorStore: vectorStore, config: config);
        var result = await tool.ExecuteAsync(InputWith("act two turning point"), CancellationToken.None);

        Assert.Contains("## Relevant world lore", result);
        Assert.Contains("### The Betrayal [Plot]", result);
        Assert.Contains($"URL: https://loreforge.example.com/world-notes/{note.Id}", result);
        Assert.Contains("The king betrays his council in act two.", result);
        Assert.DoesNotContain("## Journal observations on plot", result);
    }

    [Fact(DisplayName = "Includes journal observations section with date and content when entries are found")]
    public async Task Should_IncludeJournalObservationsSection_When_OnlyEntriesFound()
    {
        var entry = JournalEntry.Create(
            null,
            null,
            JournalSource.PlainText,
            "The pacing drags in the middle — the hero needs a setback here.",
            null,
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([entry]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("midpoint pacing"), CancellationToken.None);

        Assert.Contains("## Journal observations on plot", result);
        Assert.Contains(entry.CreatedAt.ToString("yyyy-MM-dd"), result);
        Assert.Contains("The pacing drags in the middle", result);
        Assert.DoesNotContain("## Relevant world lore", result);
    }

    [Fact(DisplayName = "Includes both sections when notes and entries are found")]
    public async Task Should_IncludeBothSections_When_BothCollectionsReturnResults()
    {
        var note = WorldNote.Create(WorldNoteCategory.Lore, "The Order", "A secret society pulling strings from the shadows.", []).Value;
        var entry = JournalEntry.Create(
            null,
            null,
            JournalSource.PlainText,
            "Foreshadow the Order earlier — their reveal feels abrupt.",
            null,
            []).Value;

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([note]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([entry]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("secret faction reveal"), CancellationToken.None);

        Assert.Contains("## Relevant world lore", result);
        Assert.Contains("### The Order [Lore]", result);
        Assert.Contains("## Journal observations on plot", result);
        Assert.Contains("Foreshadow the Order earlier", result);
    }

    [Fact(DisplayName = "Embeds the plot question before searching")]
    public async Task Should_EmbedThePlotQuestion_When_Executed()
    {
        var embeddingService = Substitute.For<IEmbeddingService>();
        embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(QueryEmbedding);

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        vectorStore.SearchJournalEntriesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(embedding: embeddingService, vectorStore: vectorStore);
        await tool.ExecuteAsync(InputWith("does the climax earn the setup?"), CancellationToken.None);

        await embeddingService.Received(1)
            .EmbedAsync("does the climax earn the setup?", Arg.Any<CancellationToken>());
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
            await File.WriteAllTextAsync(path, "  Always call the tool first.  ");
            var config = BuildConfig(path);
            var tool = CreateTool(config: config);

            Assert.Equal("Always call the tool first.", tool.ToolGuidance);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static AnalyzePlotStructureTool CreateTool(
        IEmbeddingService? embedding = null,
        IVectorStore? vectorStore = null,
        IConfiguration? config = null)
    {
        var embeddingService = embedding ?? Substitute.For<IEmbeddingService>();
        embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(QueryEmbedding);

        return new AnalyzePlotStructureTool(
            embeddingService,
            vectorStore ?? Substitute.For<IVectorStore>(),
            config ?? new ConfigurationBuilder().Build(),
            NullLogger<AnalyzePlotStructureTool>.Instance);
    }

    private static IConfiguration BuildConfig(string guidancePath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bedrock:ToolGuidancePaths:analyze_plot_structure"] = guidancePath
            })
            .Build();

    private static IConfiguration BuildConfigWithWebBaseUrl(string webBaseUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WebBaseUrl"] = webBaseUrl
            })
            .Build();

    private static JsonElement InputWith(string plotQuestion) =>
        JsonDocument.Parse($"{{\"plot_question\":\"{plotQuestion}\"}}").RootElement;
}
