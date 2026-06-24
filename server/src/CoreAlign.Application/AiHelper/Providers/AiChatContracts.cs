using System.Collections.Generic;

namespace CoreAlign.Application.AiHelper.Providers;

public enum AiChatRole
{
    System,
    User,
    Assistant,
    Tool
}

public sealed record AiToolCall(string Id, string Name, string ArgumentsJson);

public sealed record AiChatMessage(
    AiChatRole Role,
    string Content,
    IReadOnlyList<AiToolCall>? ToolCalls = null,
    string? ToolCallId = null);

public sealed record AiToolDefinition(
    string Name,
    string Description,
    string ParametersJsonSchema);

public sealed record AiChatRequest(
    IReadOnlyList<AiChatMessage> Messages,
    double? Temperature = null,
    int? MaxOutputTokens = null,
    IReadOnlyList<AiToolDefinition>? Tools = null);

public sealed record AiChatDelta(string Content, bool Done);

public sealed record AiChatCompletion(string Text, IReadOnlyList<AiToolCall> ToolCalls);
