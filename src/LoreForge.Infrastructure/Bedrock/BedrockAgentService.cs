using System.Globalization;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime.Documents;
using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LoreForge.Infrastructure.Bedrock;

public class BedrockAgentService(
    IAmazonBedrockRuntime client,
    IConversationRepository conversations,
    IEnumerable<IAgentTool> tools,
    IConfiguration config,
    ILogger<BedrockAgentService> logger) : IAgentService
{
    private const int MaxIterations = 10;

    private readonly string _modelId = config["Bedrock:AgentModelId"]
        ?? throw new InvalidOperationException("Bedrock:AgentModelId is not configured.");
    private readonly int _ttlDays = int.Parse(config["DynamoDB:TtlDays"] ?? "30");
    private readonly string _systemPrompt = LoadSystemPrompt(config);

    public async Task<string> ChatAsync(string conversationId, string userMessage, CancellationToken ct)
    {
        var history = await conversations.GetHistoryAsync(conversationId, ct);

        var messages = history.Select(ToBedrockMessage).ToList();
        messages.Add(new Message
        {
            Role = ConversationRole.User,
            Content = [new ContentBlock { Text = userMessage }]
        });

        await conversations.SaveMessageAsync(BuildMessage(conversationId, "user", userMessage), ct);

        var toolList = tools.ToList();
        var toolConfig = BuildToolConfig(toolList);

        logger.LogInformation(
            "Starting agent loop for {ConversationId}, model={Model}, tools={Tools}",
            conversationId, _modelId, string.Join(", ", toolList.Select(t => t.Name)));

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var response = await client.ConverseAsync(new ConverseRequest
            {
                ModelId = _modelId,
                System = [new SystemContentBlock { Text = _systemPrompt }],
                Messages = messages,
                ToolConfig = toolConfig
            }, ct);

            logger.LogInformation(
                "Bedrock response: stopReason={StopReason}, iteration={Iteration}",
                response.StopReason.Value, iteration);

            if (response.StopReason == StopReason.Tool_use)
            {
                var toolUseBlocks = response.Output.Message.Content
                    .Where(b => b.ToolUse is not null)
                    .Select(b => b.ToolUse!)
                    .ToList();

                messages.Add(response.Output.Message);

                var toolResults = new List<ContentBlock>();
                foreach (var toolUse in toolUseBlocks)
                {
                    logger.LogInformation("Executing tool {ToolName} (id={ToolId})", toolUse.Name, toolUse.ToolUseId);

                    var tool = toolList.FirstOrDefault(t => t.Name == toolUse.Name);
                    string result;

                    if (tool is null)
                    {
                        logger.LogWarning("Unknown tool requested: {ToolName}", toolUse.Name);
                        result = $"Error: tool '{toolUse.Name}' is not available.";
                    }
                    else
                    {
                        var input = DocumentToJsonElement(toolUse.Input);
                        result = await tool.ExecuteAsync(input, ct);
                    }

                    toolResults.Add(new ContentBlock
                    {
                        ToolResult = new ToolResultBlock
                        {
                            ToolUseId = toolUse.ToolUseId,
                            Content = [new ToolResultContentBlock { Text = result }]
                        }
                    });
                }

                messages.Add(new Message
                {
                    Role = ConversationRole.User,
                    Content = toolResults
                });

                continue;
            }

            var text = response.Output.Message.Content
                .FirstOrDefault(b => b.Text is not null)?.Text ?? string.Empty;

            await conversations.SaveMessageAsync(BuildMessage(conversationId, "assistant", text), ct);

            logger.LogInformation("Agent loop complete for {ConversationId} after {Iterations} iteration(s)", conversationId, iteration + 1);

            return text;
        }

        logger.LogError("Agent loop exceeded max iterations ({Max}) for {ConversationId}", MaxIterations, conversationId);
        throw new InvalidOperationException($"Agent loop exceeded {MaxIterations} iterations without a final response.");
    }

    private ConversationMessage BuildMessage(string conversationId, string role, string content) => new()
    {
        ConversationId = conversationId,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Role = role,
        Content = content,
        TtlEpoch = DateTimeOffset.UtcNow.AddDays(_ttlDays).ToUnixTimeSeconds()
    };

    private static Message ToBedrockMessage(ConversationMessage msg) => new()
    {
        Role = msg.Role == "user" ? ConversationRole.User : ConversationRole.Assistant,
        Content = [new ContentBlock { Text = msg.Content }]
    };

    private static ToolConfiguration BuildToolConfig(List<IAgentTool> toolList) => new()
    {
        Tools = toolList.Select(t => new Tool
        {
            ToolSpec = new ToolSpecification
            {
                Name = t.Name,
                Description = t.Description,
                InputSchema = new ToolInputSchema { Json = JsonElementToDocument(t.InputSchema) }
            }
        }).ToList()
    };

    private static Document JsonElementToDocument(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => new Document(element.EnumerateObject()
            .ToDictionary(p => p.Name, p => JsonElementToDocument(p.Value))),
        JsonValueKind.Array => new Document(element.EnumerateArray()
            .Select(JsonElementToDocument).ToList()),
        JsonValueKind.String => new Document(element.GetString()!),
        JsonValueKind.Number when element.TryGetInt32(out var i) => new Document(i),
        JsonValueKind.Number when element.TryGetInt64(out var l) => new Document(l),
        JsonValueKind.Number => new Document(element.GetDouble()),
        JsonValueKind.True => new Document(true),
        JsonValueKind.False => new Document(false),
        _ => new Document()
    };

    private static JsonElement DocumentToJsonElement(Document doc) =>
        JsonDocument.Parse(DocumentToJsonString(doc)).RootElement;

    private static string LoadSystemPrompt(IConfiguration config)
    {
        var path = config["Bedrock:SystemPromptPath"]
            ?? throw new InvalidOperationException("Bedrock:SystemPromptPath is not configured.");

        if (!File.Exists(path))
            throw new InvalidOperationException($"System prompt file not found: {path}");

        return File.ReadAllText(path).Trim();
    }

    private static string DocumentToJsonString(Document doc)
    {
        if (doc.IsNull()) return "null";
        if (doc.IsString()) return JsonSerializer.Serialize(doc.AsString());
        if (doc.IsInt()) return doc.AsInt().ToString();
        if (doc.IsLong()) return doc.AsLong().ToString();
        if (doc.IsDouble()) return doc.AsDouble().ToString(CultureInfo.InvariantCulture);
        if (doc.IsBool()) return doc.AsBool() ? "true" : "false";
        if (doc.IsList())
        {
            var items = doc.AsList().Select(DocumentToJsonString);
            return $"[{string.Join(",", items)}]";
        }
        if (doc.IsDictionary())
        {
            var props = doc.AsDictionary().Select(kv => $"{JsonSerializer.Serialize(kv.Key)}:{DocumentToJsonString(kv.Value)}");
            return $"{{{string.Join(",", props)}}}";
        }
        return "null";
    }
}
