using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LoreForge.Core.Entities;
using LoreForge.Core.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LoreForge.Infrastructure.DynamoDB;

public class DynamoConversationRepository(
    IAmazonDynamoDB dynamo,
    IConfiguration config,
    ILogger<DynamoConversationRepository> logger) : IConversationRepository
{
    private string TableName => config["DynamoDB:TableName"]!;

    public async Task<List<ConversationMessage>> GetHistoryAsync(string conversationId, CancellationToken ct)
    {
        logger.LogInformation("Loading conversation history for {ConversationId}", conversationId);

        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "ConversationId = :id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":id"] = new AttributeValue { S = conversationId }
            },
            ScanIndexForward = true
        }, ct);

        logger.LogInformation("Loaded {Count} messages for {ConversationId}", response.Items.Count, conversationId);

        return response.Items.Select(item => new ConversationMessage
        {
            ConversationId = item["ConversationId"].S,
            Timestamp = long.Parse(item["Timestamp"].N),
            Role = item["Role"].S,
            Content = item["Content"].S,
            TtlEpoch = long.Parse(item["TtlEpoch"].N)
        }).ToList();
    }

    public async Task SaveMessageAsync(ConversationMessage message, CancellationToken ct)
    {
        logger.LogInformation("Saving {Role} message for {ConversationId}", message.Role, message.ConversationId);

        await dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["ConversationId"] = new AttributeValue { S = message.ConversationId },
                ["Timestamp"] = new AttributeValue { N = message.Timestamp.ToString() },
                ["Role"] = new AttributeValue { S = message.Role },
                ["Content"] = new AttributeValue { S = message.Content },
                ["TtlEpoch"] = new AttributeValue { N = message.TtlEpoch.ToString() }
            }
        }, ct);
    }

    public async Task ClearHistoryAsync(string conversationId, CancellationToken ct)
    {
        logger.LogInformation("Clearing conversation history for {ConversationId}", conversationId);

        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "ConversationId = :id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":id"] = new AttributeValue { S = conversationId }
            },
            ProjectionExpression = "ConversationId, #ts",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#ts"] = "Timestamp"
            }
        }, ct);

        if (response.Items.Count == 0)
            return;

        var deletes = response.Items.Select(item => new WriteRequest
        {
            DeleteRequest = new DeleteRequest
            {
                Key = new Dictionary<string, AttributeValue>
                {
                    ["ConversationId"] = item["ConversationId"],
                    ["Timestamp"] = item["Timestamp"]
                }
            }
        }).ToList();

        await dynamo.BatchWriteItemAsync(new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                [TableName] = deletes
            }
        }, ct);

        logger.LogInformation("Cleared {Count} messages for {ConversationId}", deletes.Count, conversationId);
    }
}
