namespace CoreAlign.Application.Auth.DTOs;

public sealed record SecurityAlertEmailPayload(
    Guid UserId,
    string AlertType,
    DateTime OccurredAtUtc,
    string? IpAddress,
    string? UserAgent,
    string? Email = null);
