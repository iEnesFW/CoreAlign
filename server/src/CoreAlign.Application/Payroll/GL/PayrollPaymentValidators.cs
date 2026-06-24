using FluentValidation;

namespace CoreAlign.Application.Payroll.GL;

public class PayPayrollTaxesCommandValidator : AbstractValidator<PayPayrollTaxesCommand>
{
    public PayPayrollTaxesCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Reference).NotEmpty();
    }
}

public class PayPayrollSgkCommandValidator : AbstractValidator<PayPayrollSgkCommand>
{
    public PayPayrollSgkCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Reference).NotEmpty();
    }
}
