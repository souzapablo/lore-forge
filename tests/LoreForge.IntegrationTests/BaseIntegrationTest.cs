using Amazon.DynamoDBv2;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LoreForge.IntegrationTests;

public abstract class BaseIntegrationTest : IDisposable
{
    private static readonly float[] DefaultEmbedding =
        Enumerable.Range(0, 1024).Select(i => (float)i / 1024).ToArray();

    private readonly IServiceScope _scope;

    protected readonly HttpClient Client;
    protected readonly LoreForgeDbContext Context;
    protected readonly IEmbeddingService EmbeddingService;
    protected readonly IAgentService AgentService;
    protected readonly IAmazonDynamoDB DynamoDb;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        Context = _scope.ServiceProvider.GetRequiredService<LoreForgeDbContext>();
        Client = factory.CreateClient();
        EmbeddingService = factory.EmbeddingService;
        AgentService = factory.AgentService;
        DynamoDb = factory.DynamoDb;

        EmbeddingService.ClearReceivedCalls();
        EmbeddingService
            .EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(DefaultEmbedding);

        AgentService.ClearReceivedCalls();

        Context.Works.ExecuteDelete();
        Context.JournalEntries.ExecuteDelete();
        Context.WorldNotes.ExecuteDelete();
    }

    public void Dispose() => _scope.Dispose();
}
