using Hangfire.Dashboard;

namespace CoreAlign.API.Hangfire;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public const string RequiredRole = "TenantAdmin";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            return false;
        }
        return user.IsInRole(RequiredRole);
    }
}
