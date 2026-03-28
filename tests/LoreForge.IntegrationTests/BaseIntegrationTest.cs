using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Persistence;
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

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        Context = _scope.ServiceProvider.GetRequiredService<LoreForgeDbContext>();
        Client = factory.CreateClient();
        EmbeddingService = factory.EmbeddingService;

        EmbeddingService.ClearReceivedCalls();
        EmbeddingService
            .EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(DefaultEmbedding);
    }

    public void Dispose() => _scope.Dispose();
}
