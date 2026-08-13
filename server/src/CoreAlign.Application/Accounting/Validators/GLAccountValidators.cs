using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Treasury.Fx;
using FluentValidation;

namespace CoreAlign.Application.Accounting.Validators;

public class CreateGLAccountCommandValidator : AbstractValidator<CreateGLAccountCommand>
{
    public CreateGLAccountCommandValidator(IKnownCurrencyGuard currencyGuard)
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(32).WithMessage("Validation.CodeTooLong")
            .Matches("^[A-Za-z0-9.\\-]+$").WithMessage("Validation.InvalidAccountCode");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(200).WithMessage("Validation.NameTooLong");

        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Validation.Required")
            .Must(t => Enum.TryParse<Domain.Enums.AccountType>(t, ignoreCase: true, out _))
            .WithMessage("Validation.InvalidAccountType");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyMustBeIso")
            .MustBeAKnownCurrency(currencyGuard);
    }
}

public class UpdateGLAccountCommandValidator : AbstractValidator<UpdateGLAccountCommand>
{
    public UpdateGLAccountCommandValidator(IKnownCurrencyGuard currencyGuard)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(200).WithMessage("Validation.NameTooLong");
        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyMustBeIso")
            .MustBeAKnownCurrency(currencyGuard);
    }
}

public class SetGLAccountActiveCommandValidator : AbstractValidator<SetGLAccountActiveCommand>
{
    public SetGLAccountActiveCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteGLAccountCommandValidator : AbstractValidator<DeleteGLAccountCommand>
{
    public DeleteGLAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
