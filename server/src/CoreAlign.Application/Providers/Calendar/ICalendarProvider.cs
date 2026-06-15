namespace CoreAlign.Application.Providers.Calendar;

public interface ICalendarProvider : IExternalProvider
{
    Task<CalendarSyncResult> PushAsync(CalendarEvent ev, CalendarCredentials creds, CancellationToken ct);
    Task<CalendarSyncResult> UpdateAsync(string externalId, CalendarEvent ev, CalendarCredentials creds, CancellationToken ct);
    Task DeleteAsync(string externalId, CalendarCredentials creds, CancellationToken ct);
    Task<IReadOnlyList<CalendarEvent>> PullAsync(DateTime fromUtc, DateTime toUtc, CalendarCredentials creds, CancellationToken ct);
}

public sealed record CalendarCredentials(
    string OAuthToken,
    string RefreshToken,
    string CalendarId);

public sealed record CalendarEvent(
    string? Id,
    string Title,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    string? Description,
    string[] Attendees);

public sealed record CalendarSyncResult(
    string ExternalId,
    DateTime SyncedAtUtc,
    bool Success);
