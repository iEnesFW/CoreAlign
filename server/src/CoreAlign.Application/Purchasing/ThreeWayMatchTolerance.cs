using System.Text.Json;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Purchasing;

public sealed record ThreeWayMatchTolerance(
    bool Enabled,
    decimal QtyTolerancePercent,
    decimal QtyToleranceAbsolute,
    decimal PriceTolerancePercent,
    decimal PriceToleranceAbsolute)
{
    public static ThreeWayMatchTolerance Disabled { get; } = new(false, 0m, 0m, 0m, 0m);

    // Hold-for-approval is the chosen out-of-the-box policy: quantity may never
    // exceed what was received, while price may drift up to ~5% with a small
    // absolute floor so sub-cent rounding on cheap items never trips the gate.
    public static ThreeWayMatchTolerance EnabledDefault { get; } = new(true, 0m, 0m, 5m, 0.01m);
}

public interface ITolerancePolicyProvider
{
    Task<ThreeWayMatchTolerance> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class TolerancePolicyProvider : ITolerancePolicyProvider
{
    public const string Category = "Finance";
    public const string Key = "ThreeWayMatchTolerance";

    private readonly ITenantSettingRepository _settings;

    public TolerancePolicyProvider(ITenantSettingRepository settings) => _settings = settings;

    public async Task<ThreeWayMatchTolerance> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await _settings.GetAsync(Category, Key, cancellationToken);
        if (row is null || string.IsNullOrWhiteSpace(row.Value))
        {
            return ThreeWayMatchTolerance.EnabledDefault;
        }
        try
        {
            var parsed = JsonSerializer.Deserialize<ToleranceJson>(row.Value, JsonOptions);
            if (parsed is null) return ThreeWayMatchTolerance.EnabledDefault;
            return new ThreeWayMatchTolerance(
                parsed.Enabled,
                parsed.QtyTolerancePercent,
                parsed.QtyToleranceAbsolute,
                parsed.PriceTolerancePercent,
                parsed.PriceToleranceAbsolute);
        }
        catch (JsonException)
        {
            return ThreeWayMatchTolerance.EnabledDefault;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record ToleranceJson(
        bool Enabled,
        decimal QtyTolerancePercent,
        decimal QtyToleranceAbsolute,
        decimal PriceTolerancePercent,
        decimal PriceToleranceAbsolute);
}
