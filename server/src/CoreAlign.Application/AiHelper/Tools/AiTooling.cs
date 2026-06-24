using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper.Providers;

namespace CoreAlign.Application.AiHelper.Tools;

public sealed record AiToolContext(
    Guid? TenantId,
    Guid? UserId,
    IReadOnlyList<string> Roles,
    string Locale,
    string? PageEntityType = null,
    Guid? PageEntityId = null,
    Guid? CustomerId = null)
{
    public bool IsInternalStaff =>
        TenantId.HasValue && Roles.Contains("TenantAdmin", StringComparer.OrdinalIgnoreCase);

    public bool IsPortalCustomer =>
        CustomerId.HasValue && CustomerId.Value != Guid.Empty && !IsInternalStaff;
}

public sealed record AiToolResult(string ResultJson, bool IsError = false)
{
    public static AiToolResult Ok(string json) => new(json);

    public static AiToolResult Error(string message) =>
        new(JsonSerializer.Serialize(new { error = message }), true);
}

public static class AiToolArguments
{
    public static bool TryReadGuid(string? argumentsJson, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(propertyName, out var element)
                && Guid.TryParse(element.GetString(), out value);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string? ReadString(string? argumentsJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(propertyName, out var element)
                && element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}

public interface IAiTool
{
    string Name { get; }

    string Description { get; }

    string ParametersJsonSchema { get; }

    bool IsAvailable(AiToolContext context);

    Task<AiToolResult> ExecuteAsync(string argumentsJson, AiToolContext context, CancellationToken ct);
}

public interface IAiToolRegistry
{
    IReadOnlyList<AiToolDefinition> GetDefinitions(AiToolContext context);

    Task<AiToolResult> ExecuteAsync(AiToolCall call, AiToolContext context, CancellationToken ct);
}

public sealed class AiToolRegistry : IAiToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAiTool> _tools;

    public AiToolRegistry(IEnumerable<IAiTool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AiToolDefinition> GetDefinitions(AiToolContext context) =>
        _tools.Values
            .Where(t => t.IsAvailable(context))
            .Select(t => new AiToolDefinition(t.Name, t.Description, t.ParametersJsonSchema))
            .ToList();

    public async Task<AiToolResult> ExecuteAsync(AiToolCall call, AiToolContext context, CancellationToken ct)
    {
        if (!_tools.TryGetValue(call.Name, out var tool) || !tool.IsAvailable(context))
        {
            return AiToolResult.Error($"Tool '{call.Name}' is not available to the current user.");
        }

        return await tool.ExecuteAsync(call.ArgumentsJson, context, ct).ConfigureAwait(false);
    }
}
