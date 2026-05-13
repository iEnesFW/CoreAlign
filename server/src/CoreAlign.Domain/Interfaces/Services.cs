namespace CoreAlign.Domain.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashToken(string token);
}

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default);
    Task SendDuplicateRegistrationNoticeAsync(string email, CancellationToken cancellationToken = default);
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
