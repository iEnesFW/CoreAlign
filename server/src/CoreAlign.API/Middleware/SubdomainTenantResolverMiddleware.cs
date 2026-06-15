using CoreAlign.Application.Whitelabel;

namespace CoreAlign.API.Middleware;

public sealed class SubdomainTenantResolverMiddleware
{
    public const string SubdomainItemKey = "__resolved_subdomain";
    public const string TenantIdItemKey = "__resolved_subdomain_tenant_id";

    private readonly RequestDelegate _next;
    private readonly ILogger<SubdomainTenantResolverMiddleware> _logger;

    public SubdomainTenantResolverMiddleware(RequestDelegate next, ILogger<SubdomainTenantResolverMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantThemeRepository repo)
    {
        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host);
        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            context.Items[SubdomainItemKey] = subdomain;
            try
            {
                var theme = await repo.GetBySubdomainAsync(subdomain, context.RequestAborted);
                if (theme is not null)
                {
                    context.Items[TenantIdItemKey] = theme.TenantId;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to resolve tenant from subdomain {Subdomain}", subdomain);
            }
        }
        await _next(context);
    }

    private static string? ExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return null;
        if (System.Net.IPAddress.TryParse(host, out _)) return null;

        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;
        var sub = parts[0];
        if (sub.Equals("www", StringComparison.OrdinalIgnoreCase) || sub.Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return sub.ToLowerInvariant();
    }
}
