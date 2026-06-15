using FluentValidation;

namespace CoreAlign.Application.Consents;

public class CaptureConsentCommandValidator : AbstractValidator<CaptureConsentCommand>
{
    private static readonly string[] AllowedPurposes =
    {
        "essential",
        "analytics",
        "marketing",
        "terms",
        "kvkk",
    };

    public CaptureConsentCommandValidator()
    {
        RuleFor(x => x.Purpose)
            .NotEmpty()
            .MaximumLength(64)
            .Must(p => AllowedPurposes.Contains(p?.Trim().ToLowerInvariant()))
            .WithMessage($"Purpose must be one of: {string.Join(", ", AllowedPurposes)}");

        RuleFor(x => x.Version)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Fingerprint)
            .MaximumLength(64);
    }
}

public class WithdrawConsentCommandValidator : AbstractValidator<WithdrawConsentCommand>
{
    public WithdrawConsentCommandValidator()
    {
        RuleFor(x => x.ConsentId).NotEmpty();
    }
}
