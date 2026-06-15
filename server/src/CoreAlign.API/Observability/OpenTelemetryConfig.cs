using System.Reflection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CoreAlign.API.Observability;

public static class OpenTelemetryConfig
{
    public static WebApplicationBuilder AddCoreAlignOpenTelemetry(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection("OpenTelemetry");
        var serviceName = section.GetValue<string>("ServiceName") ?? "CoreAlign.API";
        var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var otlpEndpoint = section.GetValue<string>("OtlpEndpoint")
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        var metricsEnabled = section.GetValue<bool?>("MetricsEnabled") ?? true;
        var tracingEnabled = section.GetValue<bool?>("TracingEnabled") ?? true;
        var samplerRatio = section.GetValue<double?>("TracesSampleRatio")
            ?? (builder.Environment.IsDevelopment() ? 1.0 : 0.1);

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", builder.Environment.EnvironmentName),
                }));

        if (tracingEnabled)
        {
            otel.WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new TraceIdRatioBasedSampler(samplerRatio))
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.Filter = ctx =>
                        {
                            var path = ctx.Request.Path.Value ?? string.Empty;
                            if (path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)) return false;
                            if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)) return false;
                            return true;
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });
        }

        if (metricsEnabled)
        {
            otel.WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusExporter();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });
        }

        return builder;
    }
}
