using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime.Documents;
using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Bedrock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text.Json;

namespace LoreForge.UnitTests.Infrastructure.Bedrock;

public class BedrockAgentServiceTests
{
    private const string ModelId = "amazon.nova-micro-v1:0";

    [Fact(DisplayName = "Returns the model reply when Bedrock responds with end_turn")]
    public async Task Should_ReturnReply_When_ModelRespondsWithEndTurn()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(EndTurnResponse("Here is your inspiration!"));

        var service = CreateService(client);
        var result = await service.ChatAsync("conv-1", "Give me ideas", CancellationToken.None);

        Assert.Equal("Here is your inspiration!", result);
    }

    [Fact(DisplayName = "Executes the tool and returns the final reply when Bedrock requests tool use")]
    public async Task Should_ExecuteToolAndReturnReply_When_ModelRequestsToolUse()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                ToolUseResponse("search_inspiration", "use-1", new() { ["query"] = new Document("dragons") }),
                EndTurnResponse("Based on your logbook, here are some ideas."));

        var tool = Substitute.For<IAgentTool>();
        tool.Name.Returns("search_inspiration");
        tool.ExecuteAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns("Found: Elden Ring");

        var service = CreateService(client, tools: [tool]);
        var result = await service.ChatAsync("conv-1", "Give me ideas", CancellationToken.None);

        Assert.Equal("Based on your logbook, here are some ideas.", result);
        await tool.Received(1).ExecuteAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
        await client.Received(2).ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Saves user message before calling Bedrock")]
    public async Task Should_SaveUserMessage_BeforeCallingBedrock()
    {
        var callOrder = new List<string>();

        var conversations = Substitute.For<IConversationRepository>();
        conversations.GetHistoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        conversations.SaveMessageAsync(Arg.Any<ConversationMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("save"));

        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callOrder.Add("bedrock"); return EndTurnResponse("reply"); });

        var service = CreateService(client, conversations: conversations);
        await service.ChatAsync("conv-1", "hello", CancellationToken.None);

        Assert.Equal("save", callOrder[0]);
        Assert.Equal("bedrock", callOrder[1]);
    }

    [Fact(DisplayName = "Saves both user and assistant messages when conversation completes")]
    public async Task Should_SaveAssistantMessage_When_ConversationCompletes()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(EndTurnResponse("reply"));

        var conversations = Substitute.For<IConversationRepository>();
        conversations.GetHistoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        var service = CreateService(client, conversations: conversations);
        await service.ChatAsync("conv-1", "hello", CancellationToken.None);

        await conversations.Received(1).SaveMessageAsync(
            Arg.Is<ConversationMessage>(m => m.Role == "user"),
            Arg.Any<CancellationToken>());
        await conversations.Received(1).SaveMessageAsync(
            Arg.Is<ConversationMessage>(m => m.Role == "assistant" && m.Content == "reply"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Saves conversation metadata when history is empty")]
    public async Task Should_SaveConversationMeta_When_HistoryIsEmpty()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(EndTurnResponse("reply"));

        var conversations = Substitute.For<IConversationRepository>();
        conversations.GetHistoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        var service = CreateService(client, conversations: conversations);
        await service.ChatAsync("conv-1", "hello world", CancellationToken.None);

        await conversations.Received(1).SaveConversationMetaAsync(
            "conv-1",
            Arg.Is<string>(s => s == "hello world"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Does not save conversation metadata when history already exists")]
    public async Task Should_NotSaveConversationMeta_When_HistoryExists()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(EndTurnResponse("reply"));

        var conversations = Substitute.For<IConversationRepository>();
        var service = CreateService(client, conversations: conversations);

        conversations.GetHistoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new ConversationMessage { ConversationId = "conv-1", Role = "user", Content = "previous" }]);

        await service.ChatAsync("conv-1", "follow-up", CancellationToken.None);

        await conversations.DidNotReceive().SaveConversationMetaAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Truncates summary to 100 characters when first message is longer")]
    public async Task Should_TruncateSummary_When_FirstMessageExceeds100Characters()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(EndTurnResponse("reply"));

        var conversations = Substitute.For<IConversationRepository>();
        conversations.GetHistoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        var longMessage = new string('a', 150);
        var service = CreateService(client, conversations: conversations);
        await service.ChatAsync("conv-1", longMessage, CancellationToken.None);

        await conversations.Received(1).SaveConversationMetaAsync(
            "conv-1",
            Arg.Is<string>(s => s.Length == 100),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Strips XML tags from model reply before returning")]
    public async Task Should_StripXmlTags_When_ModelResponseContainsXmlTags()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(EndTurnResponse("<thinking>internal reasoning</thinking>\n\nActual reply."));

        var service = CreateService(client);
        var result = await service.ChatAsync("conv-1", "hello", CancellationToken.None);

        Assert.Equal("Actual reply.", result);
    }

    [Fact(DisplayName = "Saves stripped text to conversation history")]
    public async Task Should_SaveStrippedText_When_ModelResponseContainsXmlTags()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(EndTurnResponse("<thinking>internal reasoning</thinking>\n\nActual reply."));

        var conversations = Substitute.For<IConversationRepository>();
        conversations.GetHistoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        var service = CreateService(client, conversations: conversations);
        await service.ChatAsync("conv-1", "hello", CancellationToken.None);

        await conversations.Received(1).SaveMessageAsync(
            Arg.Is<ConversationMessage>(m => m.Role == "assistant" && m.Content == "Actual reply."),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Throws when the agent loop exceeds max iterations")]
    public async Task Should_Throw_When_MaxIterationsExceeded()
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        client.ConverseAsync(Arg.Any<ConverseRequest>(), Arg.Any<CancellationToken>())
            .Returns(ToolUseResponse("search_inspiration", "use-1", new() { ["query"] = new Document("q") }));

        var tool = Substitute.For<IAgentTool>();
        tool.Name.Returns("search_inspiration");
        tool.ExecuteAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns("some result");

        var service = CreateService(client, tools: [tool]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ChatAsync("conv-1", "hello", CancellationToken.None));
    }

    private static BedrockAgentService CreateService(
        IAmazonBedrockRuntime client,
        IConversationRepository? conversations = null,
        IEnumerable<IAgentTool>? tools = null)
    {
        var repo = conversations ?? Substitute.For<IConversationRepository>();
        repo.GetHistoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        return new BedrockAgentService(
            client,
            repo,
            tools ?? [],
            Config(),
            NullLogger<BedrockAgentService>.Instance);
    }

    private static ConverseResponse EndTurnResponse(string text) => new()
    {
        StopReason = StopReason.End_turn,
        Output = new ConverseOutput
        {
            Message = new Message
            {
                Role = ConversationRole.Assistant,
                Content = [new ContentBlock { Text = text }]
            }
        }
    };

    private static ConverseResponse ToolUseResponse(string toolName, string toolUseId, Dictionary<string, Document> input) => new()
    {
        StopReason = StopReason.Tool_use,
        Output = new ConverseOutput
        {
            Message = new Message
            {
                Role = ConversationRole.Assistant,
                Content =
                [
                    new ContentBlock
                    {
                        ToolUse = new ToolUseBlock
                        {
                            Name = toolName,
                            ToolUseId = toolUseId,
                            Input = new Document(input)
                        }
                    }
                ]
            }
        }
    };

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bedrock:AgentModelId"] = ModelId,
                ["DynamoDB:TtlDays"] = "30",
                ["Bedrock:SystemPromptPath"] = "assets/prompts/system-prompt.txt"
            })
            .Build();
}
