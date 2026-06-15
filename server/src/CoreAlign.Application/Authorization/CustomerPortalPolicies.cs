namespace CoreAlign.Application.Authorization;

public static class CustomerPortalPolicies
{
    public const string SelfService = "Customer.SelfService";

    public const string CustomerRole = "Customer";
    public const string PermissionClaimType = "permission";
    public const string SelfServicePermission = "Customer.SelfService";
}
