namespace CoreAlign.Application.Notifications.Smtp;

public sealed record SmtpCredentials(
    string Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? Password,
    string? FromAddress,
    string? FromName);
