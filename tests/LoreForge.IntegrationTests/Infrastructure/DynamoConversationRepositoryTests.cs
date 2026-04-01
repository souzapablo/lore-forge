using LoreForge.Core.Entities;
using LoreForge.Infrastructure.DynamoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoreForge.IntegrationTests.Infrastructure;

[Collection(PostgresCollection.Name)]
public class DynamoConversationRepositoryTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private static readonly IConfiguration Config =
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DynamoDB:TableName"] = "loreforge-conversations",
                ["DynamoDB:TtlDays"] = "30"
            })
            .Build();

    private DynamoConversationRepository CreateRepo() =>
        new(DynamoDb, Config, NullLogger<DynamoConversationRepository>.Instance);

    [Fact(DisplayName = "Returns empty list when no history exists for a conversation")]
    public async Task Should_ReturnEmptyList_When_NoHistoryExists()
    {
        var repo = CreateRepo();

        var result = await repo.GetHistoryAsync(NewId(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact(DisplayName = "Returns saved message when history is retrieved")]
    public async Task Should_ReturnSavedMessage_When_MessageIsSaved()
    {
        var repo = CreateRepo();
        var conversationId = NewId();
        var message = BuildMessage(conversationId, "user", "hello");

        await repo.SaveMessageAsync(message, CancellationToken.None);
        var result = await repo.GetHistoryAsync(conversationId, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("user", result[0].Role);
        Assert.Equal("hello", result[0].Content);
    }

    [Fact(DisplayName = "Returns messages in chronological order when history has multiple messages")]
    public async Task Should_ReturnMessagesInChronologicalOrder_When_MultipleMessagesSaved()
    {
        var repo = CreateRepo();
        var conversationId = NewId();

        await repo.SaveMessageAsync(BuildMessage(conversationId, "user", "first", timestamp: 1000), CancellationToken.None);
        await repo.SaveMessageAsync(BuildMessage(conversationId, "assistant", "second", timestamp: 2000), CancellationToken.None);
        await repo.SaveMessageAsync(BuildMessage(conversationId, "user", "third", timestamp: 3000), CancellationToken.None);

        var result = await repo.GetHistoryAsync(conversationId, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("first", result[0].Content);
        Assert.Equal("second", result[1].Content);
        Assert.Equal("third", result[2].Content);
    }

    [Fact(DisplayName = "Returns empty list after history is cleared")]
    public async Task Should_ReturnEmptyList_When_HistoryIsCleared()
    {
        var repo = CreateRepo();
        var conversationId = NewId();

        await repo.SaveMessageAsync(BuildMessage(conversationId, "user", "hello"), CancellationToken.None);
        await repo.ClearHistoryAsync(conversationId, CancellationToken.None);
        var result = await repo.GetHistoryAsync(conversationId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact(DisplayName = "Does not affect other conversations when history is cleared")]
    public async Task Should_NotAffectOtherConversations_When_HistoryIsCleared()
    {
        var repo = CreateRepo();
        var conversationA = NewId();
        var conversationB = NewId();

        await repo.SaveMessageAsync(BuildMessage(conversationA, "user", "from A"), CancellationToken.None);
        await repo.SaveMessageAsync(BuildMessage(conversationB, "user", "from B"), CancellationToken.None);

        await repo.ClearHistoryAsync(conversationA, CancellationToken.None);

        var resultA = await repo.GetHistoryAsync(conversationA, CancellationToken.None);
        var resultB = await repo.GetHistoryAsync(conversationB, CancellationToken.None);

        Assert.Empty(resultA);
        Assert.Single(resultB);
    }

    private static string NewId() => Guid.NewGuid().ToString();

    private static ConversationMessage BuildMessage(
        string conversationId, string role, string content, long timestamp = 0) => new()
    {
        ConversationId = conversationId,
        Timestamp = timestamp == 0 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : timestamp,
        Role = role,
        Content = content,
        TtlEpoch = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
    };
}
