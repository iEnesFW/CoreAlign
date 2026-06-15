using CoreAlign.Application.GlassEnclosure.Commands;
using FluentValidation;

namespace CoreAlign.Application.GlassEnclosure.Validators;

public class CreateColorOptionCommandValidator : AbstractValidator<CreateColorOptionCommand>
{
    public CreateColorOptionCommandValidator()
    {
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Data.HexColor).NotEmpty().Matches("^#?[0-9A-Fa-f]{6,8}$");
        RuleFor(x => x.Data.PriceModifierPercent).InclusiveBetween(-100m, 1000m);
    }
}

public class UpdateColorOptionCommandValidator : AbstractValidator<UpdateColorOptionCommand>
{
    public UpdateColorOptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Data.HexColor).NotEmpty().Matches("^#?[0-9A-Fa-f]{6,8}$");
    }
}

public class CreateGlassTypeCommandValidator : AbstractValidator<CreateGlassTypeCommand>
{
    public CreateGlassTypeCommandValidator()
    {
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.ThicknessMm).GreaterThan(0).LessThanOrEqualTo(60);
        RuleFor(x => x.Data.PricePerM2).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.WeightKgPerM2).GreaterThan(0);
        RuleFor(x => x.Data.AllowablePressurePa).GreaterThan(0);
        RuleFor(x => x.Data.MaxPanelAreaM2).GreaterThan(0);
        RuleFor(x => x.Data.Currency).NotEmpty().Length(3);
    }
}

public class UpdateGlassTypeCommandValidator : AbstractValidator<UpdateGlassTypeCommand>
{
    public UpdateGlassTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.ThicknessMm).GreaterThan(0).LessThanOrEqualTo(60);
        RuleFor(x => x.Data.PricePerM2).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.Currency).NotEmpty().Length(3);
    }
}

public class CreateProfileSystemCommandValidator : AbstractValidator<CreateProfileSystemCommand>
{
    public CreateProfileSystemCommandValidator()
    {
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.BrandId).NotEmpty();
        RuleFor(x => x.Data.MaxPanelWidthMm).InclusiveBetween(100, 5000);
        RuleFor(x => x.Data.MaxPanelHeightMm).InclusiveBetween(100, 5000);
        RuleFor(x => x.Data.MaxPanelWeightKg).GreaterThan(0);
        RuleFor(x => x.Data.SupportedGlassThicknesses).NotNull().Must(t => t.Count > 0);
        RuleFor(x => x.Data.SupportedOpenings).NotNull().Must(t => t.Count > 0);
    }
}

public class UpdateProfileSystemCommandValidator : AbstractValidator<UpdateProfileSystemCommand>
{
    public UpdateProfileSystemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.BrandId).NotEmpty();
    }
}

public class CreateProfileItemCommandValidator : AbstractValidator<CreateProfileItemCommand>
{
    public CreateProfileItemCommandValidator()
    {
        RuleFor(x => x.Data.SystemId).NotEmpty();
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.StockBarLengthMm).InclusiveBetween(1000, 7500);
        RuleFor(x => x.Data.WeightKgPerMeter).GreaterThan(0);
        RuleFor(x => x.Data.PricePerKg).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.Currency).NotEmpty().Length(3);
    }
}

public class CreateHardwareItemCommandValidator : AbstractValidator<CreateHardwareItemCommand>
{
    public CreateHardwareItemCommandValidator()
    {
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.BrandId).NotEmpty();
        RuleFor(x => x.Data.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Data.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.Currency).NotEmpty().Length(3);
    }
}

public class CreateHardwareKitCommandValidator : AbstractValidator<CreateHardwareKitCommand>
{
    public CreateHardwareKitCommandValidator()
    {
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.SystemId).NotEmpty();
        RuleForEach(x => x.Data.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.HardwareItemId).NotEmpty();
            item.RuleFor(i => i.QuantityFormula).NotEmpty().MaximumLength(500);
        });
    }
}

public class CreateBrandVendorCommandValidator : AbstractValidator<CreateBrandVendorCommand>
{
    public CreateBrandVendorCommandValidator()
    {
        RuleFor(x => x.Data.BrandId).NotEmpty();
        RuleFor(x => x.Data.VendorId).NotEmpty();
        RuleFor(x => x.Data.DefaultLeadTimeDays).GreaterThanOrEqualTo(0).LessThanOrEqualTo(365);
    }
}

public class CreateDiscountRuleCommandValidator : AbstractValidator<CreateDiscountRuleCommand>
{
    public CreateDiscountRuleCommandValidator()
    {
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Data.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.DiscountValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data)
            .Must(d => d.DiscountKind != Domain.Enums.DiscountKind.Percent || d.DiscountValue <= 100m)
            .WithMessage("Percent discount cannot exceed 100.");
    }
}

public class CreateGlassNotificationTemplateCommandValidator : AbstractValidator<CreateGlassNotificationTemplateCommand>
{
    public CreateGlassNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Data.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Data.Locale).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Data.BodyTemplate).NotEmpty();
    }
}

public class UpdateGlassEnclosureSettingsCoreCommandValidator : AbstractValidator<UpdateGlassEnclosureSettingsCoreCommand>
{
    public UpdateGlassEnclosureSettingsCoreCommandValidator()
    {
        RuleFor(x => x.Data.DefaultStockBarLengthMm).InclusiveBetween(1000, 12000);
        RuleFor(x => x.Data.DefaultJumboGlassWidthMm).InclusiveBetween(1000, 6000);
        RuleFor(x => x.Data.DefaultJumboGlassHeightMm).InclusiveBetween(1000, 4000);
        RuleFor(x => x.Data.SawKerfMm).InclusiveBetween(0m, 20m);
        RuleFor(x => x.Data.GlassKerfMm).InclusiveBetween(0m, 20m);
        RuleFor(x => x.Data.DefaultWastePercent).InclusiveBetween(0m, 50m);
        RuleFor(x => x.Data.DefaultMarginPercent).InclusiveBetween(0m, 500m);
        RuleFor(x => x.Data.LaborCostPerM2).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.BendRailFeePerM).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Data.BentGlassCostFactor).InclusiveBetween(1m, 10m);
    }
}

public class UpdateGlassEnclosureSettingsLocaleCommandValidator : AbstractValidator<UpdateGlassEnclosureSettingsLocaleCommand>
{
    public UpdateGlassEnclosureSettingsLocaleCommandValidator()
    {
        RuleFor(x => x.Data.DefaultLocale).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Data.DefaultCurrency).NotEmpty().Length(3);
        RuleFor(x => x.Data.DataRetentionDays).GreaterThan(0);
        RuleFor(x => x.Data.QuoteShareTokenTtlDays).GreaterThan(0).LessThanOrEqualTo(365);
    }
}
