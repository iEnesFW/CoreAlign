using CoreAlign.Application.Vendors.Commands;
using FluentValidation;

namespace CoreAlign.Application.Vendors.Validators;

public class CreateVendorCommandValidator : AbstractValidator<CreateVendorCommand>
{
    public CreateVendorCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(2).WithMessage("Validation.NameTooShort")
            .MaximumLength(200).WithMessage("Validation.NameTooLong");
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Validation.Required")
            .Must(t => Enum.TryParse<Domain.Enums.VendorType>(t, ignoreCase: true, out _))
            .WithMessage("Validation.InvalidVendorType");
        RuleFor(x => x.Code).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.Code));
        RuleFor(x => x.LegalName).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.LegalName));
        RuleFor(x => x.TradeName).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.TradeName));
        RuleFor(x => x.NationalId).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.NationalId));
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.TaxNumber));
        RuleFor(x => x.TaxOffice).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.TaxOffice));
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Validation.InvalidEmail")
            .MaximumLength(256).WithMessage("Validation.EmailTooLong")
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30).When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.Website).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Website));
        RuleFor(x => x.DefaultCurrency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyMustBeIso");
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateVendorCommandValidator : AbstractValidator<UpdateVendorCommand>
{
    public UpdateVendorCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).NotEmpty()
            .Must(t => Enum.TryParse<Domain.Enums.VendorType>(t, ignoreCase: true, out _))
            .WithMessage("Validation.InvalidVendorType");
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class ApproveVendorCommandValidator : AbstractValidator<ApproveVendorCommand>
{
    public ApproveVendorCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}

public class BlockVendorCommandValidator : AbstractValidator<BlockVendorCommand>
{
    public BlockVendorCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class SetVendorRatingCommandValidator : AbstractValidator<SetVendorRatingCommand>
{
    public SetVendorRatingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

public class CreateVendorAddressCommandValidator : AbstractValidator<CreateVendorAddressCommand>
{
    public CreateVendorAddressCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
    }
}

public class CreateVendorContactCommandValidator : AbstractValidator<CreateVendorContactCommand>
{
    public CreateVendorContactCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public class CreateVendorBankAccountCommandValidator : AbstractValidator<CreateVendorBankAccountCommand>
{
    public CreateVendorBankAccountCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccountHolder).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Iban)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(34).WithMessage("Validation.IbanTooLong");
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
