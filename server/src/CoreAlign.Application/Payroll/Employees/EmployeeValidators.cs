using FluentValidation;

namespace CoreAlign.Application.Payroll.Employees;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NationalId).NotEmpty().Length(11).Matches(@"^\d{11}$");
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.BaseSalaryGross).GreaterThan(0m);
        RuleFor(x => x.SalaryCurrency).NotEmpty().Length(3);
        RuleFor(x => x.Iban).MaximumLength(34).When(x => !string.IsNullOrWhiteSpace(x.Iban));
        RuleFor(x => x.DependentCount).GreaterThanOrEqualTo(0);
    }
}

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Iban).MaximumLength(34).When(x => !string.IsNullOrWhiteSpace(x.Iban));
        RuleFor(x => x.DependentCount).GreaterThanOrEqualTo(0);
    }
}

public class ChangeBaseSalaryCommandValidator : AbstractValidator<ChangeBaseSalaryCommand>
{
    public ChangeBaseSalaryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BaseSalaryGross).GreaterThan(0m);
        RuleFor(x => x.EffectiveDate).NotEmpty();
    }
}

public class TerminateEmployeeCommandValidator : AbstractValidator<TerminateEmployeeCommand>
{
    public TerminateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TerminationDate).NotEmpty();
    }
}

public class AddSalaryComponentCommandValidator : AbstractValidator<AddSalaryComponentCommand>
{
    public AddSalaryComponentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);
    }
}

public class UpdateSalaryComponentCommandValidator : AbstractValidator<UpdateSalaryComponentCommand>
{
    public UpdateSalaryComponentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);
    }
}

public class DeactivateSalaryComponentCommandValidator : AbstractValidator<DeactivateSalaryComponentCommand>
{
    public DeactivateSalaryComponentCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.EffectiveTo).NotEmpty();
    }
}

public class AddDeductionCommandValidator : AbstractValidator<AddDeductionCommand>
{
    public AddDeductionCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m).When(x => x.Amount.HasValue);
        RuleFor(x => x.Percent).InclusiveBetween(0m, 100m).When(x => x.Percent.HasValue);
        RuleFor(x => x.RemainingBalance).GreaterThanOrEqualTo(0m);
        RuleFor(x => x)
            .Must(x => x.Amount.HasValue ^ x.Percent.HasValue)
            .WithMessage("Exactly one of amount or percent must be set on a deduction.");
    }
}

public class UpdateDeductionCommandValidator : AbstractValidator<UpdateDeductionCommand>
{
    public UpdateDeductionCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.DeductionId).NotEmpty();
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m).When(x => x.Amount.HasValue);
        RuleFor(x => x.Percent).InclusiveBetween(0m, 100m).When(x => x.Percent.HasValue);
        RuleFor(x => x.RemainingBalance).GreaterThanOrEqualTo(0m);
        RuleFor(x => x)
            .Must(x => x.Amount.HasValue ^ x.Percent.HasValue)
            .WithMessage("Exactly one of amount or percent must be set on a deduction.");
    }
}

public class DeactivateDeductionCommandValidator : AbstractValidator<DeactivateDeductionCommand>
{
    public DeactivateDeductionCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.DeductionId).NotEmpty();
        RuleFor(x => x.EffectiveTo).NotEmpty();
    }
}
