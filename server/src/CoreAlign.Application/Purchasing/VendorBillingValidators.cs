using FluentValidation;

namespace CoreAlign.Application.Purchasing;

public class CreateVendorBillCommandValidator : AbstractValidator<CreateVendorBillCommand>
{
    public CreateVendorBillCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.BillNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Subtotal).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0m);
    }
}

public class CreateVendorPaymentCommandValidator : AbstractValidator<CreateVendorPaymentCommand>
{
    public CreateVendorPaymentCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Method).MaximumLength(40);
    }
}

public class UpdateVendorBillCommandValidator : AbstractValidator<UpdateVendorBillCommand>
{
    public UpdateVendorBillCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BillNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Subtotal).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0m);
    }
}

public class UpdateVendorPaymentCommandValidator : AbstractValidator<UpdateVendorPaymentCommand>
{
    public UpdateVendorPaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Method).MaximumLength(40);
    }
}

public class ApplyVendorPaymentCommandValidator : AbstractValidator<ApplyVendorPaymentCommand>
{
    public ApplyVendorPaymentCommandValidator()
    {
        RuleFor(x => x.VendorPaymentId).NotEmpty();
        RuleFor(x => x.VendorBillId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class VoidVendorPaymentCommandValidator : AbstractValidator<VoidVendorPaymentCommand>
{
    public VoidVendorPaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
