using LoreForge.Api.Extensions;
using LoreForge.Contracts.Agent;
using LoreForge.Core.Errors;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace LoreForge.Api.Features.Agent;

public class StartChatHandler(IAgentService agent) : IEndpoint
{
    public async Task<Result<StartChatResponse>> HandleAsync(ChatMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Result.Failure<StartChatResponse>(AgentErrors.MessageEmpty);

        var conversationId = Guid.NewGuid().ToString();
        var reply = await agent.ChatAsync(conversationId, request.Message, ct);
        return Result.Success(new StartChatResponse(conversationId, reply));
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/agent/chat", async (
            [FromBody] ChatMessageRequest request,
            [FromServices] StartChatHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToHttpResult(r => Results.Ok(r));
        });
}
