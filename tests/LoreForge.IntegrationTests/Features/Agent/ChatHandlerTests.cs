using LoreForge.Contracts.Agent;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Agent;

[Collection(PostgresCollection.Name)]
public class ChatHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact(DisplayName = "Returns 422 when message is empty on start")]
    public async Task Should_Return422_When_MessageIsEmptyOnStart()
    {
        var response = await Client.PostAsJsonAsync("/agent/chat",
            new ChatMessageRequest(Message: ""));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "Returns 422 when message is empty on continue")]
    public async Task Should_Return422_When_MessageIsEmptyOnContinue()
    {
        var response = await Client.PostAsJsonAsync("/agent/chat/some-conv",
            new ChatMessageRequest(Message: ""));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "Returns 200 with conversationId and reply when starting a new chat")]
    public async Task Should_Return200WithConversationIdAndReply_When_StartingNewChat()
    {
        AgentService
            .ChatAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Here is your inspiration!");

        var response = await Client.PostAsJsonAsync("/agent/chat",
            new ChatMessageRequest(Message: "Give me some inspiration"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StartChatResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.ConversationId);
        Assert.Equal("Here is your inspiration!", body.Reply);
    }

    [Fact(DisplayName = "Returns 200 with reply when continuing an existing chat")]
    public async Task Should_Return200WithReply_When_ContinuingChat()
    {
        AgentService
            .ChatAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Continuing the story...");

        var response = await Client.PostAsJsonAsync("/agent/chat/my-conv",
            new ChatMessageRequest(Message: "Tell me more"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.Equal("Continuing the story...", body!.Reply);
    }

    [Fact(DisplayName = "Passes server-generated conversationId to agent service on start")]
    public async Task Should_PassGeneratedConversationId_When_StartingNewChat()
    {
        AgentService
            .ChatAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("reply");

        await Client.PostAsJsonAsync("/agent/chat",
            new ChatMessageRequest(Message: "hello"));

        await AgentService.Received(1)
            .ChatAsync(Arg.Is<string>(id => IsGuid(id)), "hello", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Passes route conversationId to agent service on continue")]
    public async Task Should_PassRouteConversationId_When_ContinuingChat()
    {
        AgentService
            .ChatAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("reply");

        await Client.PostAsJsonAsync("/agent/chat/my-conv",
            new ChatMessageRequest(Message: "my message"));

        await AgentService.Received(1)
            .ChatAsync("my-conv", "my message", Arg.Any<CancellationToken>());
    }

    private static bool IsGuid(string value) => Guid.TryParse(value, out _);
}
