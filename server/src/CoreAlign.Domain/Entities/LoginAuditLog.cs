using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class LoginAuditLog
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string EmailAttempted { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public LoginResultType LoginResult { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    protected LoginAuditLog() { }

    public LoginAuditLog(string emailAttempted, LoginResultType result, Guid? userId = null, string? ipAddress = null, string? userAgent = null, string? failureReason = null)
    {
        EmailAttempted = emailAttempted;
        LoginResult = result;
        UserId = userId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        FailureReason = failureReason;
    }
}
