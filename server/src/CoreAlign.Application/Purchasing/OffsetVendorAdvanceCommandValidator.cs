using FluentValidation;

namespace CoreAlign.Application.Purchasing;

public class OffsetVendorAdvanceCommandValidator : AbstractValidator<OffsetVendorAdvanceCommand>
{
    public OffsetVendorAdvanceCommandValidator()
    {
        RuleFor(x => x.VendorPaymentId).NotEmpty();
        RuleFor(x => x.VendorBillId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
    }
}
