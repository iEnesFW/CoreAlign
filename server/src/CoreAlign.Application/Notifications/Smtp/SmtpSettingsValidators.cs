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
    }
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
