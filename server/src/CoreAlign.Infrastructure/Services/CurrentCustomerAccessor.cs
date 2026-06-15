using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Infrastructure.Services;

public class CurrentCustomerAccessor : ICurrentCustomerAccessor
{
    private readonly IPortalScopeService _portalScope;

    public CurrentCustomerAccessor(IPortalScopeService portalScope)
    {
        _portalScope = portalScope;
    }

    public Task<Guid?> GetCustomerIdAsync(CancellationToken cancellationToken = default)
        => _portalScope.TryGetCurrentCustomerIdAsync(cancellationToken);

    public async Task<Guid> GetCustomerIdOrThrowAsync(CancellationToken cancellationToken = default)
    {
        var id = await _portalScope.TryGetCurrentCustomerIdAsync(cancellationToken);
        if (id is null || id == Guid.Empty)
        {
            throw new PortalScopeNotResolvedException(
                "The current user has no active customer membership in this tenant.");
        }
        return id.Value;
    }
}
