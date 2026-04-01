using LoreForge.Api.Extensions;
using LoreForge.Contracts.Agent;
using LoreForge.Core.Ports;
using LoreForge.Core.Primitives;
using Microsoft.AspNetCore.Mvc;

namespace LoreForge.Api.Features.Agent;

public class ListConversationsHandler(IConversationRepository conversations) : IEndpoint
{
    public async Task<Result<List<ConversationSummaryDto>>> HandleAsync(CancellationToken ct)
    {
        var summaries = await conversations.ListConversationsAsync(ct);
        var dtos = summaries
            .Select(s => new ConversationSummaryDto(s.ConversationId, s.Summary, s.CreatedAt))
            .ToList();
        return Result.Success(dtos);
    }

    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/agent/conversations", async (
            [FromServices] ListConversationsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return result.ToHttpResult(r => Results.Ok(r));
        });
}
