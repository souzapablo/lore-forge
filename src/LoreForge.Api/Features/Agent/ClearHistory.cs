using LoreForge.Api.Extensions;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace LoreForge.Api.Features.Agent;

public class ClearHistoryHandler(IConversationRepository conversations) : IEndpoint
{
    public async Task<Result> HandleAsync(string conversationId, CancellationToken ct)
    {
        await conversations.ClearHistoryAsync(conversationId, ct);
        return Result.Success();
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/agent/chat/{conversationId}/history", async (
            string conversationId,
            [FromServices] ClearHistoryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(conversationId, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(result.Error);
        });
}
