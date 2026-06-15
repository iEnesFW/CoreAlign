namespace CoreAlign.Application.B2B;

/// <summary>
/// Resolves the active B2B scope (Customer or DealerAccount) for the current
/// authenticated user inside the current tenant. Portal endpoints call this
/// service so authorization is performed server-side from membership rows
/// rather than from any client-supplied id.
///
/// All members throw <see cref="CoreAlign.Domain.Exceptions.PortalScopeNotResolvedException"/>
/// when the caller is not a member of the required persona — surfaced as
/// HTTP 403 by the global exception middleware.
/// </summary>
public interface IPortalScopeService
{
    /// <summary>
    /// Returns the <c>CustomerId</c> of the active <c>CustomerUser</c> row for
    /// the current user in the current tenant. Throws when the caller has no
    /// active customer membership (i.e. is a tenant or dealer user).
    /// </summary>
    Task<Guid> GetCurrentCustomerIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <c>DealerAccountId</c> of the active <c>DealerUser</c> row
    /// for the current user in the current tenant. Throws when the caller has
    /// no active dealer membership.
    /// </summary>
    Task<Guid> GetCurrentDealerAccountIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the customer ids reachable by the current dealer user through
    /// active <c>DealerCustomerLink</c> rows. Used by the dealer (B2B) portal to
    /// scope dropdowns to the dealer's assigned customers. Returns an empty
    /// list when the dealer is not linked to any customer.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetDealerAllowedCustomerIdsAsync(CancellationToken cancellationToken = default);

    Task<Guid?> TryGetCurrentCustomerIdAsync(CancellationToken cancellationToken = default);

    Task<Guid?> TryGetCurrentDealerAccountIdAsync(CancellationToken cancellationToken = default);
}
