namespace CoreAlign.Application.Common.Storage;

public interface IVirusScanner
{
    Task<VirusScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}

public sealed record VirusScanResult(bool IsClean, string? ThreatName, string Provider)
{
    public static VirusScanResult Clean(string provider) => new(true, null, provider);
    public static VirusScanResult Infected(string provider, string threatName) => new(false, threatName, provider);
}

public sealed class VirusScanRejectedException : Exception
{
    public VirusScanRejectedException(string threatName, string provider)
        : base($"Upload rejected by {provider}: {threatName}")
    {
        ThreatName = threatName;
        Provider = provider;
    }

    public string ThreatName { get; }
    public string Provider { get; }
}
