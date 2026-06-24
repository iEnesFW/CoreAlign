using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.AiHelper.Tools;

public sealed class GetRecentErrorsTool : IAiTool
{
    private const int DefaultTake = 5;
    private const int MaxTake = 20;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IErrorLogRepository _errors;

    public GetRecentErrorsTool(IErrorLogRepository errors)
    {
        _errors = errors;
    }

    public string Name => "get_recent_errors";

    public string Description =>
        "Returns the current tenant's most recent recorded errors (time, HTTP status, method, path, exception type, message, " +
        "correlation id). Use this to diagnose 'I got an error' / 'e-invoice failed' style questions, then map the error to " +
        "the fix from the documentation. Optionally filter by part of the path or by HTTP status code.";

    public string ParametersJsonSchema =>
        """{"type":"object","properties":{"pathContains":{"type":"string","description":"Filter to errors whose request path contains this text (e.g. 'invoice', 'e-fatura' route)"},"statusCode":{"type":"integer","description":"Filter by HTTP status code (e.g. 500, 409)"},"take":{"type":"integer","description":"How many recent errors to return (default 5, max 20)"}},"required":[]}""";

    public bool IsAvailable(AiToolContext context) => context.IsInternalStaff;

    public async Task<AiToolResult> ExecuteAsync(string argumentsJson, AiToolContext context, CancellationToken ct)
    {
        if (!context.TenantId.HasValue)
        {
            return AiToolResult.Error("No tenant context is available.");
        }

        var (pathContains, statusCode, take) = ParseArguments(argumentsJson);

        try
        {
            var query = new ErrorLogQuery(
                TenantId: context.TenantId,
                Severity: null,
                Source: null,
                StatusCode: statusCode,
                CorrelationId: null,
                PathContains: pathContains,
                UserId: null,
                OnlyUnresolved: null,
                FromUtc: null,
                ToUtc: null,
                Search: null,
                Skip: 0,
                Take: take);

            var (items, total) = await _errors.QueryAsync(query, ct).ConfigureAwait(false);

            var projected = items.Select(e => new
            {
                occurredAtUtc = e.OccurredAtUtc,
                statusCode = e.StatusCode,
                httpMethod = e.HttpMethod,
                path = e.Path,
                exceptionType = e.ExceptionType,
                message = e.Message,
                correlationId = e.CorrelationId,
                clientPage = e.ClientPage,
            });

            return AiToolResult.Ok(JsonSerializer.Serialize(new { total, errors = projected }, JsonOptions));
        }
        catch (Exception)
        {
            return AiToolResult.Error("Recent errors could not be retrieved.");
        }
    }

    private static (string? PathContains, int? StatusCode, int Take) ParseArguments(string argumentsJson)
    {
        string? pathContains = null;
        int? statusCode = null;
        var take = DefaultTake;

        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return (pathContains, statusCode, take);
        }

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (pathContains, statusCode, take);
            }

            if (doc.RootElement.TryGetProperty("pathContains", out var p) && p.ValueKind == JsonValueKind.String)
            {
                pathContains = p.GetString();
            }
            if (doc.RootElement.TryGetProperty("statusCode", out var s) && s.ValueKind == JsonValueKind.Number && s.TryGetInt32(out var sc))
            {
                statusCode = sc;
            }
            if (doc.RootElement.TryGetProperty("take", out var t) && t.ValueKind == JsonValueKind.Number && t.TryGetInt32(out var tk))
            {
                take = Math.Clamp(tk, 1, MaxTake);
            }
        }
        catch (JsonException)
        {
        }

        return (pathContains, statusCode, take);
    }
}
