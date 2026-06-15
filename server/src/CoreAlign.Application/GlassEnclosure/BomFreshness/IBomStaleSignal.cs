namespace CoreAlign.Application.GlassEnclosure.BomFreshness;

public interface IBomStaleSignal
{
    Task SignalStaleAsync(Guid projectId, BomStaleReason reason, CancellationToken cancellationToken = default);
    Task SignalFreshAsync(Guid projectId, CancellationToken cancellationToken = default);
}

public enum BomStaleReason
{
    RunChanged = 0,
    PanelChanged = 1,
    HardwareChanged = 2,
    PriceListChanged = 3,
    GlassChanged = 4,
    ManualOverride = 5,
    SurveyApplied = 6
}
