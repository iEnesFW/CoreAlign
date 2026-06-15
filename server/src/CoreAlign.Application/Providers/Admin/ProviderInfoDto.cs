using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Providers.Admin;

public sealed record ProviderInfoDto(
    string Name,
    string DisplayName,
    string Category,
    bool IsConfigured,
    bool IsEnabled,
    bool IsDefault,
    bool IsSandbox,
    string LastHealthStatus,
    string? LastHealthMessage,
    DateTime? LastHealthCheckedUtc,
    DateTime? LastUsedAtUtc,
    IReadOnlyList<string> Capabilities);

public sealed record ProviderHealthSummaryDto(
    string ProviderName,
    string Category,
    bool IsHealthy,
    string? Message,
    long ResponseTimeMs,
    DateTime CheckedAtUtc,
    string? EndpointProbed,
    int? HttpStatusCode);

public sealed record ProviderTestRunStepResult(
    string StepName,
    bool Passed,
    string? Detail,
    long DurationMs);

public sealed record ProviderTestRunResultDto(
    string ProviderName,
    string Category,
    bool Sandbox,
    bool AllPassed,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    IReadOnlyList<ProviderTestRunStepResult> Steps);

public sealed record ProviderAuditEventDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? BeforeJson,
    string? AfterJson,
    Guid? UserId,
    DateTime ChangedAtUtc,
    Guid? CorrelationId,
    long Sequence);

public sealed record ProviderWebhookHistoryItemDto(
    Guid Id,
    string ProviderName,
    ProviderCategory Category,
    string EventType,
    string Status,
    string? ProcessingError,
    int RetryCount,
    DateTime ReceivedAtUtc,
    DateTime? ProcessedAtUtc);

