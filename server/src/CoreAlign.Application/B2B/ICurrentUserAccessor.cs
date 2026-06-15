using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.B2B;

/// <summary>
/// Returns the authenticated user id of the current request — used by portal
/// handlers that need to stamp <c>OriginCustomerUserId</c> / <c>OriginDealerUserId</c>
/// on entities or attribute an approval/rejection without trusting any
/// client-supplied id. Infrastructure resolves this from the JWT
/// <c>sub</c> claim via <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>Returns the current user id or <c>null</c> when the request is unauthenticated.</summary>
    Guid? UserId { get; }

    /// <summary>Returns the current user id or throws <see cref="PortalScopeNotResolvedException"/>.</summary>
    Guid UserIdOrThrow();
}
