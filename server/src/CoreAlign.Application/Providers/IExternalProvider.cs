namespace CoreAlign.Application.Providers;

public interface IExternalProvider
{
    string Name { get; }
    string DisplayName { get; }
    ProviderCapabilities Capabilities { get; }

    Task<ProviderHealthCheckResult> CheckHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(ProviderHealthCheckResult.NotImplemented(Name));
}

public sealed record ProviderHealthCheckResult(
    string ProviderName,
    bool IsHealthy,
    string? Message,
    TimeSpan ResponseTime,
    DateTime CheckedAtUtc,
    string? EndpointProbed = null,
    int? HttpStatusCode = null)
{
    public static ProviderHealthCheckResult Healthy(string providerName, TimeSpan elapsed, string? endpointProbed = null, int? httpStatusCode = null) =>
        new(providerName, true, null, elapsed, DateTime.UtcNow, endpointProbed, httpStatusCode);

    public static ProviderHealthCheckResult Unhealthy(string providerName, string message, TimeSpan elapsed, string? endpointProbed = null, int? httpStatusCode = null) =>
        new(providerName, false, message, elapsed, DateTime.UtcNow, endpointProbed, httpStatusCode);

    public static ProviderHealthCheckResult NotImplemented(string providerName) =>
        new(providerName, false, "Health check is not implemented for this provider.", TimeSpan.Zero, DateTime.UtcNow);
}
