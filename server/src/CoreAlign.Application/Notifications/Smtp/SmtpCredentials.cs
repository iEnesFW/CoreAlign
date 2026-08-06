namespace CoreAlign.Application.Notifications.Smtp;

public sealed record SmtpCredentials(
    string Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? Password,
    string? FromAddress,
    string? FromName,
    string? AuthMode = null,
    string? OAuthProvider = null,
    string? OAuthTenantId = null,
    string? OAuthClientId = null,
    string? OAuthClientSecret = null,
    string? OAuthRefreshToken = null,
    string? OAuthTokenEndpoint = null,
    string? OAuthScope = null)
{
    public bool UsesOAuth =>
        string.Equals(AuthMode, SmtpAuthModes.OAuth2, StringComparison.OrdinalIgnoreCase);
}
