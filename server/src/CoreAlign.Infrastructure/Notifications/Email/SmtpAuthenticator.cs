using CoreAlign.Application.Notifications.Smtp;
using CoreAlign.Domain.Exceptions;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace CoreAlign.Infrastructure.Notifications.Email;

internal static class SmtpAuthenticator
{
    public static async Task AuthenticateAsync(
        SmtpClient client,
        SmtpCredentials credentials,
        ISmtpAccessTokenProvider tokenProvider,
        CancellationToken cancellationToken)
    {
        if (credentials.UsesOAuth)
        {
            var account = FirstNonEmpty(credentials.Username, credentials.FromAddress);
            if (account is null)
            {
                throw new SmtpOAuthConfigurationException(
                    "A mailbox address is required for XOAUTH2 authentication.");
            }
            var settings = SmtpOAuthResolver.Resolve(credentials);
            var token = await tokenProvider.GetAccessTokenAsync(settings, cancellationToken).ConfigureAwait(false);
            await client
                .AuthenticateAsync(new SaslMechanismOAuth2(account, token), cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(credentials.Username))
        {
            await client
                .AuthenticateAsync(credentials.Username, credentials.Password ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }
        return null;
    }
}
