namespace CoreAlign.API.Observability;

public static class SentryStartupExtensions
{
    public static WebApplicationBuilder AddCoreAlignSentry(this WebApplicationBuilder builder)
    {
        var dsn = builder.Configuration["Sentry:Dsn"];

        builder.WebHost.UseSentry(options =>
        {
            options.Dsn = dsn ?? string.Empty;
            options.Environment = builder.Environment.EnvironmentName;
            options.AttachStacktrace = true;
            options.SendDefaultPii = false;
            options.MaxRequestBodySize = Sentry.Extensibility.RequestSize.None;
            options.TracesSampleRate = builder.Configuration.GetValue<double?>("Sentry:TracesSampleRate") ?? 0.0;
            options.SetBeforeSend(SentryPiiScrubber.Scrub);
        });

        return builder;
    }
}
