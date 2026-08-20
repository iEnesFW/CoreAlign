using System;
using CoreAlign.Domain.Enums;
using FluentValidation;

namespace CoreAlign.Application.Payroll.Runs;

public class CreatePayrollRunCommandValidator : AbstractValidator<CreatePayrollRunCommand>
{
    public CreatePayrollRunCommandValidator()
    {
        RuleFor(x => x.PeriodYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12);
        // Turkish statutory payroll: the minimum wage, the SGK ceiling and the income-tax
        // brackets are all TRY amounts, and the GL posting carries no exchange rate. A run in
        // any other currency computes meaningless tax and books foreign amounts as if they
        // were lira, so it is refused at the boundary rather than silently misstated.
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => string.Equals(c?.Trim(), "TRY", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Validation.PayrollCurrencyMustBeTry");

        // Off-cycle runs do not stack the year-to-date ladder: PostPayrollRunHandler skips the
        // advance when an employee already has this month posted, so a bonus run's income-tax
        // base never enters the cumulative total and every later month under-withholds. The
        // duplicate-period guard is keyed by run type, so a Regular + an OffCycle run for the
        // same month is otherwise accepted. Refused until the ladder stacks within a month.
        RuleFor(x => x.RunType)
            .Equal(PayrollRunType.Regular)
            .WithMessage("Validation.PayrollOffCycleNotSupported");
    }
}

public class CalculatePayrollRunCommandValidator : AbstractValidator<CalculatePayrollRunCommand>
{
    public CalculatePayrollRunCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ApprovePayrollRunCommandValidator : AbstractValidator<ApprovePayrollRunCommand>
{
    public ApprovePayrollRunCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ReopenPayrollRunCommandValidator : AbstractValidator<ReopenPayrollRunCommand>
{
    public ReopenPayrollRunCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class PostPayrollRunCommandValidator : AbstractValidator<PostPayrollRunCommand>
{
    public PostPayrollRunCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class PayPayrollRunCommandValidator : AbstractValidator<PayPayrollRunCommand>
{
    public PayPayrollRunCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
