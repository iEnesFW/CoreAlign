using FluentValidation;

namespace CoreAlign.Application.Notifications.Smtp;

public sealed class UpsertTenantSmtpSettingsCommandValidator : AbstractValidator<UpsertTenantSmtpSettingsCommand>
{
    public UpsertTenantSmtpSettingsCommandValidator()
    {
        RuleFor(x => x.Host).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Username).MaximumLength(320);
        RuleFor(x => x.FromName).MaximumLength(128);
        RuleFor(x => x.FromAddress)
            .EmailAddress()
            .MaximumLength(254)
            .When(x => !string.IsNullOrWhiteSpace(x.FromAddress));

        RuleFor(x => x.AuthMode)
            .Must(SmtpAuthModes.IsKnown)
            .WithMessage("Authentication mode must be 'Password' or 'OAuth2'.");

        When(IsOAuth, () =>
        {
            RuleFor(x => x.OAuthProvider)
                .NotEmpty()
                .Must(SmtpOAuthProviders.IsKnown)
                .WithMessage("OAuth provider must be 'Google', 'Microsoft' or 'Custom'.");

            RuleFor(x => x.OAuthClientId).NotEmpty().MaximumLength(255);
            RuleFor(x => x.OAuthTenantId).MaximumLength(128);
            RuleFor(x => x.OAuthScope).MaximumLength(500);
            RuleFor(x => x.OAuthTokenEndpoint).MaximumLength(500);

            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("A mailbox address is required for XOAUTH2 authentication.")
                .When(x => string.IsNullOrWhiteSpace(x.FromAddress));

            RuleFor(x => x.OAuthTokenEndpoint)
                .NotEmpty()
                .WithMessage("A token endpoint is required for a custom OAuth provider.")
                .When(x => string.Equals(x.OAuthProvider, SmtpOAuthProviders.Custom, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static bool IsOAuth(UpsertTenantSmtpSettingsCommand command) =>
        string.Equals(command.AuthMode, SmtpAuthModes.OAuth2, StringComparison.OrdinalIgnoreCase);
}

public sealed class SendTestEmailCommandValidator : AbstractValidator<SendTestEmailCommand>
{
    public SendTestEmailCommandValidator()
    {
        RuleFor(x => x.ToAddress)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254)
            .Must(NoHeaderInjection).WithMessage("Recipient address contains invalid characters.");
    }

    private static bool NoHeaderInjection(string value) =>
        value is not null && !value.Any(c => c is '\r' or '\n');
}
