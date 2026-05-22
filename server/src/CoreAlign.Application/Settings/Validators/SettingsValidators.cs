using CoreAlign.Application.Settings.Commands;
using FluentValidation;

namespace CoreAlign.Application.Settings.Validators;

public class UpdateCompanyProfileCommandValidator : AbstractValidator<UpdateCompanyProfileCommand>
{
    public UpdateCompanyProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithMessage("Validation.NameRequired");
        RuleFor(x => x.DefaultCurrency)
            .NotEmpty().Length(3).WithMessage("Validation.CurrencyMustBeIso");
        RuleFor(x => x.ReportingCurrency).Length(3).When(x => !string.IsNullOrEmpty(x.ReportingCurrency));
        RuleFor(x => x.LocaleCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.FiscalYearStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.LogoUrl).MaximumLength(500);
        RuleFor(x => x.PrimaryColor).MaximumLength(16);
        RuleFor(x => x.SecondaryColor).MaximumLength(16);
    }
}

public class UpsertTenantSettingsCommandValidator : AbstractValidator<UpsertTenantSettingsCommand>
{
    public UpsertTenantSettingsCommandValidator()
    {
        RuleFor(x => x.Items).NotNull().Must(i => i.Count > 0).WithMessage("Validation.NoItemsToUpsert");
        RuleForEach(x => x.Items).SetValidator(new SettingUpsertItemValidator());
    }
}

public class SettingUpsertItemValidator : AbstractValidator<SettingUpsertItem>
{
    public SettingUpsertItemValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Key).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DataType).NotEmpty().MaximumLength(16);
    }
}

public class CreateEmailTemplateCommandValidator : AbstractValidator<CreateEmailTemplateCommand>
{
    public CreateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64).Matches("^[A-Za-z0-9_.-]+$").WithMessage("Validation.InvalidTemplateCode");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
    }
}

public class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
    }
}
