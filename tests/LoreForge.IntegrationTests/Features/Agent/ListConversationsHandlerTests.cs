using LoreForge.Contracts.Agent;
using LoreForge.Infrastructure.DynamoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;

namespace LoreForge.IntegrationTests.Features.Agent;

[Collection(PostgresCollection.Name)]
public class ListConversationsHandlerTests(IntegrationTestWebAppFactory factory)
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

    [Fact(DisplayName = "Returns 200 with empty list when no conversations exist")]
    public async Task Should_Return200WithEmptyList_When_NoConversationsExist()
    {
        var response = await Client.GetAsync("/agent/conversations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ConversationSummaryDto>>();
        Assert.NotNull(body);
    }

    [Fact(DisplayName = "Returns saved conversation in list")]
    public async Task Should_ReturnConversation_When_MetaWasSaved()
    {
        var repo = CreateRepo();
        var conversationId = Guid.NewGuid().ToString();
        await repo.SaveConversationMetaAsync(conversationId, "Test question", 1000, CancellationToken.None);

        var response = await Client.GetAsync("/agent/conversations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ConversationSummaryDto>>();
        Assert.Contains(body!, c => c.ConversationId == conversationId && c.Summary == "Test question");
    }

    [Fact(DisplayName = "Returns conversations newest first")]
    public async Task Should_ReturnConversationsNewestFirst_When_MultipleExist()
    {
        var repo = CreateRepo();
        var older = Guid.NewGuid().ToString();
        var newer = Guid.NewGuid().ToString();

        await repo.SaveConversationMetaAsync(older, "older question", 1000, CancellationToken.None);
        await repo.SaveConversationMetaAsync(newer, "newer question", 2000, CancellationToken.None);

        var response = await Client.GetAsync("/agent/conversations");
        var body = await response.Content.ReadFromJsonAsync<List<ConversationSummaryDto>>();

        var olderIndex = body!.FindIndex(c => c.ConversationId == older);
        var newerIndex = body.FindIndex(c => c.ConversationId == newer);

        Assert.True(newerIndex < olderIndex);
    }
}
