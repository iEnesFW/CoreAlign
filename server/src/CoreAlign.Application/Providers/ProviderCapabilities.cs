namespace CoreAlign.Application.Providers;

[Flags]
public enum ProviderCapability
{
    None = 0,
    Invoice = 1,
    Despatch = 2,
    ProducerReceipt = 4,
    Archive = 8,
    Cancel = 16,
    Refund = 32,
    WebhookCallback = 64,
    BulkSend = 128,
    SignatureValidation = 256,
    OAuth = 512,
    Webhook = 1024,
    RealTimeStatus = 2048
}

public sealed record ProviderCapabilities(
    ProviderCapability Flags,
    IReadOnlyDictionary<string, string> Metadata)
{
    public static ProviderCapabilities Empty => new(ProviderCapability.None, new Dictionary<string, string>());
    public bool Has(ProviderCapability cap) => (Flags & cap) == cap;
}
