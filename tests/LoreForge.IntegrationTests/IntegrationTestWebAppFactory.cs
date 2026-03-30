using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace LoreForge.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg17")
        .Build();

    public IEmbeddingService EmbeddingService { get; } = Substitute.For<IEmbeddingService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<LoreForgeDbContext>));

            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<LoreForgeDbContext>(options =>
                options.UseNpgsql(_container.GetConnectionString(), o => o.UseVector())
                       .UseSnakeCaseNamingConvention());

            var embeddingDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IEmbeddingService));

            if (embeddingDescriptor is not null)
                services.Remove(embeddingDescriptor);

            services.AddSingleton(EmbeddingService);
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LoreForgeDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    public const string Name = "Postgres";
}
