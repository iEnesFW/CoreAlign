using CoreAlign.Application.Common.Storage;

namespace CoreAlign.Infrastructure.Storage;

public sealed class NoOpVirusScanner : IVirusScanner
{
    public const string ProviderName = "NoOp";

    public Task<VirusScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(VirusScanResult.Clean(ProviderName));
    }
}
