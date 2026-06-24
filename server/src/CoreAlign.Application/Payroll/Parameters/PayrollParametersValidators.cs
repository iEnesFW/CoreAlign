using FluentValidation;

namespace CoreAlign.Application.Payroll.Parameters;

public class CreatePayrollParametersCommandValidator : AbstractValidator<CreatePayrollParametersCommand>
{
    public CreatePayrollParametersCommandValidator()
    {
        RuleFor(x => x.EffectiveYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.SgkEmployeeRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.SgkEmployerRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.SgkEmployer5PointIncentiveRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.UnemploymentEmployeeRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.UnemploymentEmployerRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.StampTaxRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.SgkFloorMonthly).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.SgkCeilingMonthly).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.SgkCeilingMultiplier).GreaterThan(0m);
        RuleFor(x => x.GrossMinimumWage).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);
        RuleFor(x => x.TaxBrackets).NotEmpty();
        RuleForEach(x => x.TaxBrackets).ChildRules(b =>
        {
            b.RuleFor(t => t.RatePercent).InclusiveBetween(0m, 100m);
            b.RuleFor(t => t.UpperBound).GreaterThan(0m).When(t => t.UpperBound.HasValue);
        });
    }
}

public class UpdatePayrollParametersCommandValidator : AbstractValidator<UpdatePayrollParametersCommand>
{
    public UpdatePayrollParametersCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SgkEmployeeRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.SgkEmployerRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.SgkEmployer5PointIncentiveRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.UnemploymentEmployeeRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.UnemploymentEmployerRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.StampTaxRate).InclusiveBetween(0m, 1m);
        RuleFor(x => x.SgkFloorMonthly).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.SgkCeilingMonthly).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.SgkCeilingMultiplier).GreaterThan(0m);
        RuleFor(x => x.GrossMinimumWage).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);
    }
}
