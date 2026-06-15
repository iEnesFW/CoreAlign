using CoreAlign.Application.Accounting.Commands;
using FluentValidation;

namespace CoreAlign.Application.Accounting.Validators;

public class CreateAccountingPeriodCommandValidator : AbstractValidator<CreateAccountingPeriodCommand>
{
    public CreateAccountingPeriodCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("Validation.YearOutOfRange");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Validation.MonthOutOfRange");
    }
}

public class ClosePeriodCommandValidator : AbstractValidator<ClosePeriodCommand>
{
    public ClosePeriodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class ReopenPeriodCommandValidator : AbstractValidator<ReopenPeriodCommand>
{
    public ReopenPeriodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class LockPeriodCommandValidator : AbstractValidator<LockPeriodCommand>
{
    public LockPeriodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
