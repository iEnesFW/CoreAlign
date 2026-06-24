using System.Diagnostics.Metrics;

namespace CoreAlign.Infrastructure.Observability;

public static class ErrorLogMetrics
{
    public const string MeterName = "CoreAlign.ErrorLog";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Persisted = Meter.CreateCounter<long>("errorlog_persisted_total");
    private static readonly Counter<long> WriteFailed = Meter.CreateCounter<long>("errorlog_write_failed_total");

    public static void RecordPersisted(string severity, string source) =>
        Persisted.Add(1,
            new KeyValuePair<string, object?>("severity", severity),
            new KeyValuePair<string, object?>("source", source));

    public static void RecordWriteFailure() => WriteFailed.Add(1);
}
