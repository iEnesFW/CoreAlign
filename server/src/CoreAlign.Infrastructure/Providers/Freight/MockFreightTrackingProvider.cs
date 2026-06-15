using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Freight;

namespace CoreAlign.Infrastructure.Providers.Freight.Mock;

public sealed class MockFreightTrackingProvider : IFreightTrackingProvider
{
    public string Name => "mock";

    public string DisplayName => "Mock Freight Tracking Provider";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.WebhookCallback,
        new Dictionary<string, string> { ["env"] = "dev" });

    public Task<FreightTrackResult> TrackAsync(string trackingNumber, FreightCredentials creds, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var events = new List<FreightEvent>
        {
            new(FreightStatus.Created, now, "MOCK-ORIGIN")
        };
        var result = new FreightTrackResult(
            trackingNumber,
            FreightStatus.Created,
            now,
            now.AddDays(3),
            events);
        return Task.FromResult(result);
    }

    public Task<FreightLabelResult?> CreateShipmentAsync(FreightShipmentRequest req, FreightCredentials creds, CancellationToken ct)
    {
        var tracking = $"MOCK-FREIGHT-{Guid.NewGuid()}";
        var result = new FreightLabelResult(
            tracking,
            Array.Empty<byte>(),
            0m,
            "TRY");
        return Task.FromResult<FreightLabelResult?>(result);
    }
}
