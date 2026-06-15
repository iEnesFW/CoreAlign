using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Common;

/// <summary>
/// Base controller for B2B identity endpoints. Authorization is performed by the
/// command/query handler via <see cref="CoreAlign.Domain.Interfaces.IB2BAuthorizationService"/>
/// because ownership (CustomerOwner / DealerOwner) is derived from membership rows
/// and cannot be expressed as a static role claim. Controllers therefore use
/// only <c>[Authorize]</c> and forward the resolved caller identity + roles to
/// the handler, which decides whether the operation is allowed for the target
/// customer or dealer.
/// </summary>
public abstract class B2BControllerBase : ControllerBase
{
    protected Guid CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    protected IReadOnlyList<string> CurrentRoles() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
}
