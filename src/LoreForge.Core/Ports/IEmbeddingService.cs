namespace LoreForge.Core.Ports;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
}
