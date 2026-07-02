using CoreAlign.Application.Payments.Commands;
using FluentValidation;

namespace CoreAlign.Application.Payments.Validators;

public class OffsetCustomerAdvanceCommandValidator : AbstractValidator<OffsetCustomerAdvanceCommand>
{
    public OffsetCustomerAdvanceCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Applications).NotEmpty();
        RuleForEach(x => x.Applications).ChildRules(line =>
        {
            line.RuleFor(l => l.InvoiceId).NotEmpty();
            line.RuleFor(l => l.AppliedAmount).GreaterThan(0m);
        });
    }
}
