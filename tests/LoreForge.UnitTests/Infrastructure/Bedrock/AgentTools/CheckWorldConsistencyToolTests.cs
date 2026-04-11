using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Bedrock.AgentTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text.Json;

namespace LoreForge.UnitTests.Infrastructure.Bedrock.AgentTools;

public class CheckWorldConsistencyToolTests
{
    private static readonly float[] QueryEmbedding = [0.1f, 0.2f];

    [Fact(DisplayName = "Returns no results message when no world notes match")]
    public async Task Should_ReturnNoResultsMessage_When_NoWorldNotesMatch()
    {
        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("dragons"), CancellationToken.None);

        Assert.Equal("No relevant world notes found for this consistency check.", result);
    }

    [Fact(DisplayName = "Includes header, title, category, url and content in output when notes are found")]
    public async Task Should_IncludeNoteDetails_When_WorldNotesFound()
    {
        var note = MakeNote(WorldNoteCategory.Character, "Eldrin", "An elven mage who cannot cast fire spells.");

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([note]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("fire magic"), CancellationToken.None);

        Assert.Contains("Established world lore", result);
        Assert.Contains("Title: Eldrin", result);
        Assert.Contains("Category: Character", result);
        Assert.Contains($"URL: /world-notes/{note.Id}", result);
        Assert.Contains("Content: An elven mage who cannot cast fire spells.", result);
    }

    [Fact(DisplayName = "Formats each note with labeled fields")]
    public async Task Should_FormatNoteWithLabeledFields_When_WorldNotesFound()
    {
        var note = MakeNote(WorldNoteCategory.Location, "The Ashwood", "A cursed forest east of the capital.");

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([note]);

        var tool = CreateTool(vectorStore: vectorStore);
        var result = await tool.ExecuteAsync(InputWith("forest"), CancellationToken.None);

        Assert.Contains("Title: The Ashwood", result);
        Assert.Contains("Category: Location", result);
        Assert.Contains($"URL: /world-notes/{note.Id}", result);
        Assert.Contains("Content: A cursed forest east of the capital.", result);
    }

    [Fact(DisplayName = "Embeds the query before searching world notes")]
    public async Task Should_EmbedTheQuery_When_Executed()
    {
        var embeddingService = Substitute.For<IEmbeddingService>();
        embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(QueryEmbedding);

        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchWorldNotesAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tool = CreateTool(embedding: embeddingService, vectorStore: vectorStore);
        await tool.ExecuteAsync(InputWith("fire magic rules"), CancellationToken.None);

        await embeddingService.Received(1)
            .EmbedAsync("fire magic rules", Arg.Any<CancellationToken>());
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

    private static CheckWorldConsistencyTool CreateTool(
        IEmbeddingService? embedding = null,
        IVectorStore? vectorStore = null,
        IConfiguration? config = null)
    {
        var embeddingService = embedding ?? Substitute.For<IEmbeddingService>();
        embeddingService.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(QueryEmbedding);

        return new CheckWorldConsistencyTool(
            embeddingService,
            vectorStore ?? Substitute.For<IVectorStore>(),
            config ?? new ConfigurationBuilder().Build(),
            NullLogger<CheckWorldConsistencyTool>.Instance);
    }

    private static IConfiguration BuildConfig(string guidancePath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bedrock:ToolGuidancePaths:check_world_consistency"] = guidancePath
            })
            .Build();

    private static JsonElement InputWith(string query) =>
        JsonDocument.Parse($"{{\"query\":\"{query}\"}}").RootElement;

    private static WorldNote MakeNote(WorldNoteCategory category, string title, string content) =>
        WorldNote.Create(category, title, content, []).Value;
}
