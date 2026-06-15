namespace CoreAlign.Application.Providers.LaserMeter;

public interface ILaserMeterAdapter : IExternalProvider
{
    LaserMeterTransport Transport { get; }

    Task<LaserMeasurement> ParsePayloadAsync(LaserMeterRawFrame frame, CancellationToken cancellationToken);
}

public enum LaserMeterTransport
{
    WebBluetoothGatt = 0,
    UsbHid = 1,
    SerialBle = 2,
    ManualEntry = 3
}

public enum LaserMeasurementKind
{
    Distance = 0,
    Angle = 1,
    Area = 2,
    Volume = 3,
    Inclination = 4
}

public sealed record LaserMeterRawFrame(
    byte[] RawBytes,
    DateTime CapturedAtUtc,
    string DeviceSerial);

public sealed record LaserMeasurement(
    LaserMeasurementKind Kind,
    double Value,
    string Unit,
    double? Accuracy,
    DateTime CapturedUtc,
    string DeviceSerial);
