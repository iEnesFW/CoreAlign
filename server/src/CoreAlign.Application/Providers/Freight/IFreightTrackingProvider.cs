namespace CoreAlign.Application.Providers.Freight;

public enum FreightStatus
{
    Created,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    Failed,
    Cancelled
}

public sealed record FreightCredentials(string ApiKey, string ClientId);

public sealed record FreightEvent(FreightStatus Status, DateTime Timestamp, string Location);

public sealed record FreightTrackResult(
    string TrackingNumber,
    FreightStatus Status,
    DateTime LastUpdateUtc,
    DateTime? EstimatedDelivery,
    IReadOnlyList<FreightEvent> Events);

public sealed record FreightShipmentRequest(
    string FromAddress,
    string ToAddress,
    decimal WeightKg,
    string DimensionsCm,
    string? Notes);

public sealed record FreightLabelResult(
    string TrackingNumber,
    byte[] LabelPdfBytes,
    decimal EstimatedCost,
    string Currency);

public interface IFreightTrackingProvider : IExternalProvider
{
    Task<FreightTrackResult> TrackAsync(string trackingNumber, FreightCredentials creds, CancellationToken ct);
    Task<FreightLabelResult?> CreateShipmentAsync(FreightShipmentRequest req, FreightCredentials creds, CancellationToken ct);
}
