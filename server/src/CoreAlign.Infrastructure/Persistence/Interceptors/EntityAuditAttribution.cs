using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Infrastructure.Persistence.Interceptors;

public static class EntityAuditAttribution
{
    public static Guid ResolveAttributedTenantId(object entity, Guid fallback)
    {
        if (entity is Tenant tenant && tenant.Id != Guid.Empty)
        {
            return tenant.Id;
        }
        if (entity is TenantEntity owned && owned.TenantId != Guid.Empty)
        {
            return owned.TenantId;
        }
        return fallback;
    }
}
