using FluentValidation;

namespace CoreAlign.Application.Identity.Locale;

public sealed class SetPreferredLocaleValidator : AbstractValidator<SetPreferredLocaleCommand>
{
    public SetPreferredLocaleValidator()
    {
        RuleFor(x => x.Locale)
            .NotEmpty()
            .MaximumLength(10);
    }
}
