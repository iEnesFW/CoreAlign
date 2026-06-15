using CoreAlign.Domain.Entities.Privacy;

namespace CoreAlign.Application.Privacy;

public interface IRetentionPolicyService
{
    Task<IReadOnlyList<RetentionPolicyDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<RetentionPolicyDto> CreateAsync(
        UpsertRetentionPolicyInput input,
        CancellationToken cancellationToken = default);

    Task<RetentionPolicyDto> UpdateAsync(
        Guid policyId,
        UpsertRetentionPolicyInput input,
        CancellationToken cancellationToken = default);
}

public sealed record UpsertRetentionPolicyInput(
    string EntityType,
    int RetentionDays,
    RetentionActionOnExpiry ActionOnExpiry,
    bool KeepFinancialTrail,
    bool IsEnabled);

public sealed record RetentionPolicyDto(
    Guid Id,
    Guid TenantId,
    string EntityType,
    int RetentionDays,
    RetentionActionOnExpiry ActionOnExpiry,
    bool KeepFinancialTrail,
    bool IsEnabled,
    DateTime? LastRunAtUtc,
    int LastRunAffectedCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
