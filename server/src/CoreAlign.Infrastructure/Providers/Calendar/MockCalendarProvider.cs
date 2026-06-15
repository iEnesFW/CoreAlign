using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Calendar;

namespace CoreAlign.Infrastructure.Providers.Calendar.Mock;

public sealed class MockCalendarProvider : ICalendarProvider
{
    public string Name => "mock";
    public string DisplayName => "Mock Calendar Provider";
    public ProviderCapabilities Capabilities => new(
        ProviderCapability.OAuth,
        new Dictionary<string, string> { ["env"] = "dev" });

    public Task<CalendarSyncResult> PushAsync(CalendarEvent ev, CalendarCredentials creds, CancellationToken ct)
    {
        var externalId = $"mock-cal-{Guid.NewGuid()}";
        return Task.FromResult(new CalendarSyncResult(externalId, DateTime.UtcNow, true));
    }

    public Task<CalendarSyncResult> UpdateAsync(string externalId, CalendarEvent ev, CalendarCredentials creds, CancellationToken ct)
    {
        return Task.FromResult(new CalendarSyncResult(externalId, DateTime.UtcNow, true));
    }

    public Task DeleteAsync(string externalId, CalendarCredentials creds, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CalendarEvent>> PullAsync(DateTime fromUtc, DateTime toUtc, CalendarCredentials creds, CancellationToken ct)
    {
        IReadOnlyList<CalendarEvent> empty = Array.Empty<CalendarEvent>();
        return Task.FromResult(empty);
    }
}
