using FluentValidation;

namespace CoreAlign.Application.Payroll.Runs;

public class CreatePayrollRunCommandValidator : AbstractValidator<CreatePayrollRunCommand>
{
    public CreatePayrollRunCommandValidator()
    {
        RuleFor(x => x.PeriodYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.PeriodMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
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
