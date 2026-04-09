using System.Text.Json;

namespace LoreForge.Core.Ports;

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }
    string? ToolGuidance => null;
    Task<string> ExecuteAsync(JsonElement input, CancellationToken ct);
}
