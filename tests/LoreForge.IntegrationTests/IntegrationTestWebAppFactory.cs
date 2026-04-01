using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.DynamoDb;
using Testcontainers.PostgreSql;

namespace LoreForge.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .Build();

    private readonly DynamoDbContainer _dynamoDb = new DynamoDbBuilder("amazon/dynamodb-local:latest")
        .Build();

    public IEmbeddingService EmbeddingService { get; } = Substitute.For<IEmbeddingService>();
    public IAgentService AgentService { get; } = Substitute.For<IAgentService>();
    public IAmazonDynamoDB DynamoDb { get; private set; } = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<LoreForgeDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<LoreForgeDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString(), o => o.UseVector())
                       .UseSnakeCaseNamingConvention());

            var embeddingDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IEmbeddingService));
            if (embeddingDescriptor is not null)
                services.Remove(embeddingDescriptor);
            services.AddSingleton(EmbeddingService);

            var agentDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IAgentService));
            if (agentDescriptor is not null)
                services.Remove(agentDescriptor);
            services.AddSingleton(AgentService);

            var dynamoDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IAmazonDynamoDB));
            if (dynamoDescriptor is not null)
                services.Remove(dynamoDescriptor);
            services.AddSingleton<IAmazonDynamoDB>(_ => DynamoDb);
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _dynamoDb.StartAsync());

        // DynamoDb must be set before Services is accessed for the first time
        DynamoDb = new AmazonDynamoDBClient(
            new BasicAWSCredentials("dummy", "dummy"),
            new AmazonDynamoDBConfig
            {
                ServiceURL = _dynamoDb.GetConnectionString()
            });

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LoreForgeDbContext>();
        await db.Database.MigrateAsync();

        await CreateConversationsTableAsync();
    }

    public new async Task DisposeAsync()
    {
        DynamoDb.Dispose();
        await _postgres.DisposeAsync();
        await _dynamoDb.DisposeAsync();
        await base.DisposeAsync();
    }

    private async Task CreateConversationsTableAsync()
    {
        await DynamoDb.CreateTableAsync(new CreateTableRequest
        {
            TableName = "loreforge-conversations",
            KeySchema =
            [
                new KeySchemaElement("ConversationId", KeyType.HASH),
                new KeySchemaElement("Timestamp", KeyType.RANGE)
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition("ConversationId", ScalarAttributeType.S),
                new AttributeDefinition("Timestamp", ScalarAttributeType.N)
            ],
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }
}

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    public const string Name = "Postgres";
}
