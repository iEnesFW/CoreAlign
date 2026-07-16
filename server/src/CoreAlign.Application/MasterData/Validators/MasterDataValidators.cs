using CoreAlign.Application.MasterData.Commands;
using FluentValidation;

namespace CoreAlign.Application.MasterData.Validators;

internal static class IbanValidation
{
    public static bool IsValidChecksum(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            return true;
        }
        var cleaned = iban.Replace(" ", string.Empty).ToUpperInvariant();
        if (cleaned.Length is < 5 or > 34)
        {
            return false;
        }
        var rearranged = cleaned[4..] + cleaned[..4];
        var remainder = 0;
        foreach (var ch in rearranged)
        {
            if (ch is >= '0' and <= '9')
            {
                remainder = (remainder * 10 + (ch - '0')) % 97;
            }
            else if (ch is >= 'A' and <= 'Z')
            {
                remainder = (remainder * 100 + (ch - 'A' + 10)) % 97;
            }
            else
            {
                return false;
            }
        }
        return remainder == 1;
    }
}

public class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).Matches(@"^[A-Za-z0-9_\-\.]+$")
            .WithMessage("Validation.CodeFormat");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class CreateProductCategoryCommandValidator : AbstractValidator<CreateProductCategoryCommand>
{
    public CreateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpdateProductCategoryCommandValidator : AbstractValidator<UpdateProductCategoryCommand>
{
    public UpdateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ParentCategoryId)
            .NotEqual(x => x.Id).When(x => x.ParentCategoryId.HasValue)
            .WithMessage("Validation.CategoryCannotBeOwnParent");
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class CreateUnitOfMeasureCommandValidator : AbstractValidator<CreateUnitOfMeasureCommand>
{
    public CreateUnitOfMeasureCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Symbol).MaximumLength(16);
        RuleFor(x => x.ConversionFactor).GreaterThan(0m).WithMessage("Validation.MustBePositive");
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 8);
    }
}

public class UpdateUnitOfMeasureCommandValidator : AbstractValidator<UpdateUnitOfMeasureCommand>
{
    public UpdateUnitOfMeasureCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ConversionFactor).GreaterThan(0m).WithMessage("Validation.MustBePositive");
        RuleFor(x => x.BaseUomId)
            .NotEqual(x => x.Id).When(x => x.BaseUomId.HasValue)
            .WithMessage("Validation.UoMCannotBeOwnBase");
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 8);
    }
}

public class CreateTaxRateCommandValidator : AbstractValidator<CreateTaxRateCommand>
{
    public CreateTaxRateCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.RatePercent).InclusiveBetween(0m, 100m)
            .WithMessage("Validation.TaxRateRange");
        RuleFor(x => x.CountryCode).MaximumLength(3);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpdateTaxRateCommandValidator : AbstractValidator<UpdateTaxRateCommand>
{
    public UpdateTaxRateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.RatePercent).InclusiveBetween(0m, 100m)
            .WithMessage("Validation.TaxRateRange");
    }
}

public class CreatePaymentTermCommandValidator : AbstractValidator<CreatePaymentTermCommand>
{
    public CreatePaymentTermCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NetDays).GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");
        RuleFor(x => x.DiscountDays).GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0m, 100m)
            .WithMessage("Validation.DiscountPercentRange");
        RuleFor(x => x.DiscountDays).LessThanOrEqualTo(x => x.NetDays)
            .When(x => x.DiscountDays > 0)
            .WithMessage("Validation.DiscountDaysExceedsNetDays");
    }
}

public class UpdatePaymentTermCommandValidator : AbstractValidator<UpdatePaymentTermCommand>
{
    public UpdatePaymentTermCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NetDays).GreaterThanOrEqualTo(0).WithMessage("Validation.NonNegative");
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0m, 100m)
            .WithMessage("Validation.DiscountPercentRange");
    }
}

public class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches(@"^[A-Z]{3}$")
            .WithMessage("Validation.CurrencyIsoFormat");
        RuleFor(x => x.ValidUntilUtc)
            .GreaterThan(x => x.ValidFromUtc!.Value)
            .When(x => x.ValidFromUtc.HasValue && x.ValidUntilUtc.HasValue)
            .WithMessage("Validation.ValidUntilMustBeAfterValidFrom");
    }
}

public class UpdatePriceListCommandValidator : AbstractValidator<UpdatePriceListCommand>
{
    public UpdatePriceListCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.ValidUntilUtc)
            .GreaterThan(x => x.ValidFromUtc!.Value)
            .When(x => x.ValidFromUtc.HasValue && x.ValidUntilUtc.HasValue)
            .WithMessage("Validation.ValidUntilMustBeAfterValidFrom");
    }
}

public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.AddressLine1).MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(3);
        RuleFor(x => x.Phone).MaximumLength(40);
    }
}

public class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountCommandValidator()
    {
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BranchName).MaximumLength(100);
        RuleFor(x => x.Iban).NotEmpty().MaximumLength(42)
            .Matches("^[A-Za-z]{2}[0-9]{2}[A-Za-z0-9 ]{1,38}$").WithMessage("Validation.IbanInvalid")
            .Must(IbanValidation.IsValidChecksum).WithMessage("Validation.IbanInvalid");
        RuleFor(x => x.Swift).MaximumLength(11);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class UpdateBankAccountCommandValidator : AbstractValidator<UpdateBankAccountCommand>
{
    public UpdateBankAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BranchName).MaximumLength(100);
        RuleFor(x => x.Iban).NotEmpty().MaximumLength(42)
            .Matches("^[A-Za-z]{2}[0-9]{2}[A-Za-z0-9 ]{1,38}$").WithMessage("Validation.IbanInvalid")
            .Must(IbanValidation.IsValidChecksum).WithMessage("Validation.IbanInvalid");
        RuleFor(x => x.Swift).MaximumLength(11);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CreateCustomerGroupCommandValidator : AbstractValidator<CreateCustomerGroupCommand>
{
    public CreateCustomerGroupCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DefaultDiscountPercent).InclusiveBetween(0m, 100m)
            .WithMessage("Validation.DiscountPercentRange");
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpdateCustomerGroupCommandValidator : AbstractValidator<UpdateCustomerGroupCommand>
{
    public UpdateCustomerGroupCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DefaultDiscountPercent).InclusiveBetween(0m, 100m)
            .WithMessage("Validation.DiscountPercentRange");
    }
}
