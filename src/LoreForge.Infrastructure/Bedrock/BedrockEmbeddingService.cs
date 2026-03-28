using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using LoreForge.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LoreForge.Infrastructure.Bedrock;

public class BedrockEmbeddingService(
    IAmazonBedrockRuntime client,
    IConfiguration configuration,
    ILogger<BedrockEmbeddingService> logger) : IEmbeddingService
{
    private readonly string _modelId = configuration["Bedrock:EmbeddingModelId"]
        ?? throw new InvalidOperationException("Bedrock:EmbeddingModelId is not configured.");

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new { inputText = text });

        var response = await client.InvokeModelAsync(new InvokeModelRequest
        {
            ModelId = _modelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(body)
        }, ct);

        using var doc = await JsonDocument.ParseAsync(response.Body, cancellationToken: ct);

        var embedding = doc.RootElement
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();

        logger.LogInformation("Bedrock embedding: model={Model}, dimensions={Dimensions}", _modelId, embedding.Length);

        return embedding;
    }
}
