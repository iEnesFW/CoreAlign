using Microsoft.AspNetCore.Authorization;

namespace CoreAlign.API.Authorization;

public static class PersonaPolicies
{
    public const string PersonaClaimType = "persona";

    public const string CustomerPersonaValue = "customer";
    public const string DealerPersonaValue = "dealer";
    public const string TenantPersonaValue = "tenant";

    public const string Customer = "CustomerPersona";
    public const string Dealer = "DealerPersona";
    public const string Tenant = "TenantPersona";

    public const string PlatformAdminRole = "PlatformAdmin";
    public const string TenantAdminRole = "TenantAdmin";
    public const string PlatformAdmin = "PlatformAdminPolicy";

    public static AuthorizationPolicy CustomerPolicy =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(PersonaClaimType, CustomerPersonaValue)
            .Build();

    public static AuthorizationPolicy DealerPolicy =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(PersonaClaimType, DealerPersonaValue)
            .Build();

    public static AuthorizationPolicy TenantPolicy =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(PersonaClaimType, TenantPersonaValue)
            .Build();

    public static AuthorizationPolicy PlatformAdminPolicy =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(PlatformAdminRole)
            .Build();
}
