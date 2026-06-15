namespace CoreAlign.Domain.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, IEnumerable<string> roles);
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, IEnumerable<string> roles, string? persona, DateTime? mfaVerifiedAtUtc = null);
    string GenerateRefreshToken();
    string HashToken(string token);
}

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default);
    Task SendDuplicateRegistrationNoticeAsync(string email, CancellationToken cancellationToken = default);
    Task SendSecurityAlertAsync(object payload, CancellationToken cancellationToken = default);
    Task SendInvoiceIssuedAsync(string email, string invoiceNumber, string customerName, decimal total, string currency, CancellationToken cancellationToken = default);
    Task SendOrderCommentPostedAsync(string email, string authorPersona, string body, CancellationToken cancellationToken = default);
    Task SendDealerOrderPendingApprovalAsync(string email, string dealerName, int lineCount, decimal total, string currency, CancellationToken cancellationToken = default);
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
