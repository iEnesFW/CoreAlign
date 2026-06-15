namespace CoreAlign.Domain.Enums;

public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    Sms = 2,
    Push = 3,
    WhatsApp = 4
}

public enum NotificationStatus
{
    Pending = 0,
    Queued = 1,
    Sending = 2,
    Sent = 3,
    Delivered = 4,
    Failed = 5,
    Bounced = 6,
    Read = 7
}
