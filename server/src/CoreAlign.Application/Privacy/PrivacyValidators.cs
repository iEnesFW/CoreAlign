using FluentValidation;

namespace CoreAlign.Application.Privacy;

public class EraseMyAccountCommandValidator : AbstractValidator<EraseMyAccountCommand>
{
    public EraseMyAccountCommandValidator()
    {
        RuleFor(x => x.ConfirmationUsername)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public class EraseCustomerByAdminCommandValidator : AbstractValidator<EraseCustomerByAdminCommand>
{
    public EraseCustomerByAdminCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ConfirmationUsername)
            .NotEmpty()
            .MaximumLength(100);
    }
}
