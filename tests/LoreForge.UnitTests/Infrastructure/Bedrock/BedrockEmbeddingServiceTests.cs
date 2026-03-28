using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using LoreForge.Infrastructure.Bedrock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace LoreForge.UnitTests.Infrastructure.Bedrock;

public class BedrockEmbeddingServiceTests
{
    private const string ModelId = "amazon.titan-embed-text-v2:0";

    private static IConfiguration ConfigWith(string modelId) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bedrock:EmbeddingModelId"] = modelId
            })
            .Build();

    private static IAmazonBedrockRuntime ClientReturning(float[] embedding)
    {
        var client = Substitute.For<IAmazonBedrockRuntime>();
        var body = JsonSerializer.SerializeToUtf8Bytes(new { embedding });
        client.InvokeModelAsync(Arg.Any<InvokeModelRequest>(), Arg.Any<CancellationToken>())
              .Returns(new InvokeModelResponse { Body = new MemoryStream(body) });
        return client;
    }

    private static BedrockEmbeddingService CreateService(IAmazonBedrockRuntime client, IConfiguration config) =>
        new(client, config, Substitute.For<ILogger<BedrockEmbeddingService>>());

    [Fact(DisplayName = "Returns the embedding array parsed from the Bedrock response")]
    public async Task Should_ReturnParsedEmbedding_When_BedrockResponds()
    {
        var expected = new float[] { 0.1f, 0.5f, 0.9f };
        var service = CreateService(ClientReturning(expected), ConfigWith(ModelId));

        var result = await service.EmbedAsync("some text", CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Uses the model ID from configuration when calling Bedrock")]
    public async Task Should_UseConfiguredModelId_When_CallingBedrock()
    {
        var client = ClientReturning([0.1f]);
        var service = CreateService(client, ConfigWith(ModelId));

        await service.EmbedAsync("hello", CancellationToken.None);

        await client.Received(1).InvokeModelAsync(
            Arg.Is<InvokeModelRequest>(r => r.ModelId == ModelId),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Sends the input text in the request body when calling Bedrock")]
    public async Task Should_SendInputTextInBody_When_CallingBedrock()
    {
        var client = ClientReturning([0.1f]);
        var service = CreateService(client, ConfigWith(ModelId));
        const string inputText = "dragons and magic";

        await service.EmbedAsync(inputText, CancellationToken.None);

        await client.Received(1).InvokeModelAsync(
            Arg.Is<InvokeModelRequest>(r => ContainsInputText(r.Body, inputText)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Throws on construction when the model ID is not in configuration")]
    public void Should_Throw_When_ModelIdIsNotConfigured()
    {
        Assert.Throws<InvalidOperationException>(
            () => CreateService(Substitute.For<IAmazonBedrockRuntime>(), new ConfigurationBuilder().Build()));
    }

    private static bool ContainsInputText(Stream body, string expected)
    {
        body.Position = 0;
        using var doc = JsonDocument.Parse(body);
        body.Position = 0;
        return doc.RootElement.GetProperty("inputText").GetString() == expected;
    }
}
