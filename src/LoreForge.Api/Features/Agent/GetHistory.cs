using LoreForge.Api.Extensions;
using LoreForge.Contracts.Agent;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace LoreForge.Api.Features.Agent;

public class GetHistoryHandler(IConversationRepository conversations) : IEndpoint
{
    public async Task<Result<List<ConversationMessageDto>>> HandleAsync(string conversationId, CancellationToken ct)
    {
        var messages = await conversations.GetHistoryAsync(conversationId, ct);
        var dtos = messages
            .Select(m => new ConversationMessageDto(m.Role, m.Content, m.Timestamp))
            .ToList();
        return Result.Success(dtos);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/agent/chat/{conversationId}/history", async (
            string conversationId,
            [FromServices] GetHistoryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(conversationId, ct);
            return result.ToHttpResult(r => Results.Ok(r));
        });
}
