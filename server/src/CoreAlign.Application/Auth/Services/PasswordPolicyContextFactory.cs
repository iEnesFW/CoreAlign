using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Auth.Services;

public static class PasswordPolicyContextFactory
{
    public const string TenantAdminRole = "TenantAdmin";

    public static PasswordPolicyContext For(User user) =>
        IsTenantAdmin(user) ? PasswordPolicyContext.TenantAdmin : PasswordPolicyContext.Standard;

    private static bool IsTenantAdmin(User user) =>
        user.UserRoles is not null
        && user.UserRoles.Any(ur => string.Equals(ur.Role?.Name, TenantAdminRole, StringComparison.OrdinalIgnoreCase));
}
