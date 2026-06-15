using CoreAlign.Domain.Entities.Privacy;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Privacy;

public class RetentionPolicyService : IRetentionPolicyService
{
    private readonly IRetentionPolicyRepository _repository;
    private readonly ITenantContext _tenant;

    public RetentionPolicyService(
        IRetentionPolicyRepository repository,
        ITenantContext tenant)
    {
        _repository = repository;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<RetentionPolicyDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var items = await _repository.ListByTenantAsync(tenantId, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<RetentionPolicyDto> CreateAsync(
        UpsertRetentionPolicyInput input,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var entity = RetentionPolicy.Create(
            tenantId,
            input.EntityType,
            input.RetentionDays,
            input.ActionOnExpiry,
            input.KeepFinancialTrail);

        await _repository.AddAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    public async Task<RetentionPolicyDto> UpdateAsync(
        Guid policyId,
        UpsertRetentionPolicyInput input,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(policyId, cancellationToken)
            ?? throw new RetentionPolicyNotFoundException();

        entity.Update(
            input.RetentionDays,
            input.ActionOnExpiry,
            input.KeepFinancialTrail,
            input.IsEnabled,
            DateTime.UtcNow);

        _repository.Update(entity);
        return ToDto(entity);
    }

    private static RetentionPolicyDto ToDto(RetentionPolicy entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.EntityType,
            entity.RetentionDays,
            entity.ActionOnExpiry,
            entity.KeepFinancialTrail,
            entity.IsEnabled,
            entity.LastRunAtUtc,
            entity.LastRunAffectedCount,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
}
