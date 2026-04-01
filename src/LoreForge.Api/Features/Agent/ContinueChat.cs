using LoreForge.Api.Extensions;
using LoreForge.Contracts.Agent;
using LoreForge.Core.Errors;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace LoreForge.Api.Features.Agent;

public class ContinueChatHandler(IAgentService agent) : IEndpoint
{
    public async Task<Result<ChatResponse>> HandleAsync(string conversationId, ChatMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Result.Failure<ChatResponse>(AgentErrors.MessageEmpty);

        var reply = await agent.ChatAsync(conversationId, request.Message, ct);
        return Result.Success(new ChatResponse(reply));
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/agent/chat/{conversationId}", async (
            string conversationId,
            [FromBody] ChatMessageRequest request,
            [FromServices] ContinueChatHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(conversationId, request, ct);
            return result.ToHttpResult(r => Results.Ok(r));
        });
}
