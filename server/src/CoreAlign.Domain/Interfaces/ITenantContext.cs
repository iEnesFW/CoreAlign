namespace CoreAlign.Domain.Interfaces;

public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
    bool HasTenant { get; }
}
