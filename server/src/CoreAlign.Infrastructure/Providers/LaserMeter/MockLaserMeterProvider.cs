using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.LaserMeter;

namespace CoreAlign.Infrastructure.Providers.LaserMeter;

public sealed class ManualLaserMeterAdapter : ILaserMeterAdapter
{
    public string Name => "mock";
    public string DisplayName => "Mock Manual Laser Meter";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.None,
        new Dictionary<string, string> { ["env"] = "dev", ["transport"] = "manual" });

    public LaserMeterTransport Transport => LaserMeterTransport.ManualEntry;

    public Task<LaserMeasurement> ParsePayloadAsync(LaserMeterRawFrame frame, CancellationToken cancellationToken)
    {
        var measurement = new LaserMeasurement(
            LaserMeasurementKind.Distance,
            1234.5,
            "mm",
            0.5,
            frame.CapturedAtUtc,
            frame.DeviceSerial);
        return Task.FromResult(measurement);
    }
}

public sealed class MockBluetoothLaserMeterAdapter : ILaserMeterAdapter
{
    public string Name => "mock-bluetooth";
    public string DisplayName => "Mock Bluetooth Laser Meter";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.None,
        new Dictionary<string, string> { ["env"] = "dev", ["transport"] = "ble-gatt" });

    public LaserMeterTransport Transport => LaserMeterTransport.WebBluetoothGatt;

    public Task<LaserMeasurement> ParsePayloadAsync(LaserMeterRawFrame frame, CancellationToken cancellationToken)
    {
        var deterministicValue = frame.RawBytes is { Length: > 0 }
            ? 1000.0 + (frame.RawBytes[0] * 1.5)
            : 1500.0;

        var measurement = new LaserMeasurement(
            LaserMeasurementKind.Distance,
            deterministicValue,
            "mm",
            1.0,
            frame.CapturedAtUtc,
            frame.DeviceSerial);
        return Task.FromResult(measurement);
    }
}
