using System.Net;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Notifications.Smtp;

public static class SmtpAuthModes
{
    public const string Password = "Password";
    public const string OAuth2 = "OAuth2";

    public static bool IsKnown(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || string.Equals(value, Password, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, OAuth2, StringComparison.OrdinalIgnoreCase);
}

public static class SmtpOAuthProviders
{
    public const string Google = "Google";
    public const string Microsoft = "Microsoft";
    public const string Custom = "Custom";

    public static bool IsKnown(string? value) =>
        string.Equals(value, Google, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Microsoft, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Custom, StringComparison.OrdinalIgnoreCase);
}

public static class SmtpOAuthGrantTypes
{
    public const string RefreshToken = "refresh_token";
    public const string ClientCredentials = "client_credentials";
}

public sealed record SmtpOAuthSettings(
    string TokenEndpoint,
    string GrantType,
    string ClientId,
    string? ClientSecret,
    string? RefreshToken,
    string? Scope);

public static class SmtpOAuthResolver
{
    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GoogleScope = "https://mail.google.com/";
    private const string MicrosoftRefreshScope = "offline_access https://outlook.office.com/SMTP.Send";
    private const string MicrosoftAppScope = "https://outlook.office365.com/.default";

    public static SmtpOAuthSettings Resolve(SmtpCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var provider = string.IsNullOrWhiteSpace(credentials.OAuthProvider)
            ? SmtpOAuthProviders.Custom
            : credentials.OAuthProvider.Trim();
        if (!SmtpOAuthProviders.IsKnown(provider))
        {
            throw new SmtpOAuthConfigurationException($"Unsupported OAuth provider '{provider}'.");
        }

        var clientId = Trimmed(credentials.OAuthClientId);
        if (clientId is null)
        {
            throw new SmtpOAuthConfigurationException("OAuth client id is required.");
        }

        var refreshToken = Trimmed(credentials.OAuthRefreshToken);
        var clientSecret = Trimmed(credentials.OAuthClientSecret);
        var grantType = refreshToken is not null
            ? SmtpOAuthGrantTypes.RefreshToken
            : SmtpOAuthGrantTypes.ClientCredentials;

        if (grantType == SmtpOAuthGrantTypes.ClientCredentials && clientSecret is null)
        {
            throw new SmtpOAuthConfigurationException(
                "A refresh token or a client secret is required to obtain an access token.");
        }

        var endpoint = ResolveEndpoint(provider, credentials);
        var scope = Trimmed(credentials.OAuthScope) ?? DefaultScope(provider, grantType);

        return new SmtpOAuthSettings(endpoint, grantType, clientId, clientSecret, refreshToken, scope);
    }

    private static string ResolveEndpoint(string provider, SmtpCredentials credentials)
    {
        var configured = Trimmed(credentials.OAuthTokenEndpoint);
        if (configured is not null)
        {
            return ValidateEndpoint(configured);
        }

        if (string.Equals(provider, SmtpOAuthProviders.Google, StringComparison.OrdinalIgnoreCase))
        {
            return GoogleTokenEndpoint;
        }

        if (string.Equals(provider, SmtpOAuthProviders.Microsoft, StringComparison.OrdinalIgnoreCase))
        {
            var directory = Trimmed(credentials.OAuthTenantId) ?? "common";
            if (directory.Contains('/', StringComparison.Ordinal) || directory.Contains('\\', StringComparison.Ordinal))
            {
                throw new SmtpOAuthConfigurationException("Directory (tenant) id contains invalid characters.");
            }
            return ValidateEndpoint(
                $"https://login.microsoftonline.com/{Uri.EscapeDataString(directory)}/oauth2/v2.0/token");
        }

        throw new SmtpOAuthConfigurationException("A token endpoint is required for a custom OAuth provider.");
    }

    private static string? DefaultScope(string provider, string grantType)
    {
        if (string.Equals(provider, SmtpOAuthProviders.Google, StringComparison.OrdinalIgnoreCase))
        {
            return GoogleScope;
        }
        if (string.Equals(provider, SmtpOAuthProviders.Microsoft, StringComparison.OrdinalIgnoreCase))
        {
            return grantType == SmtpOAuthGrantTypes.RefreshToken ? MicrosoftRefreshScope : MicrosoftAppScope;
        }
        return null;
    }

    public static string ValidateEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new SmtpOAuthConfigurationException("Token endpoint must be an absolute URL.");
        }
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new SmtpOAuthConfigurationException("Token endpoint must use https.");
        }
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new SmtpOAuthConfigurationException("Token endpoint must not embed credentials.");
        }
        if (IsInternalHost(uri))
        {
            throw new SmtpOAuthConfigurationException("Token endpoint must not point at an internal address.");
        }
        return uri.ToString();
    }

    private static bool IsInternalHost(Uri uri)
    {
        if (uri.IsLoopback) return true;

        var host = uri.DnsSafeHost;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)) return true;

        if (!IPAddress.TryParse(host, out var address)) return false;
        return IsPrivate(address);
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal) return true;
            if (address.IsIPv4MappedToIPv6) return IsPrivate(address.MapToIPv4());
            var v6 = address.GetAddressBytes();
            return (v6[0] & 0xFE) == 0xFC;
        }

        var bytes = address.GetAddressBytes();
        if (bytes[0] == 10) return true;
        if (bytes[0] == 127) return true;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
        return bytes[0] == 0;
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public interface ISmtpAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(SmtpOAuthSettings settings, CancellationToken cancellationToken = default);
}
