using CoreAlign.Application.Manufacturing.Commands;
using FluentValidation;

namespace CoreAlign.Application.Manufacturing.Validators;

public class CreateWorkCenterOperatorValidator : AbstractValidator<CreateWorkCenterOperatorCommand>
{
    public CreateWorkCenterOperatorValidator()
    {
        RuleFor(c => c.WorkCenterId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.QualificationLevel).IsInEnum();
        RuleFor(c => c.CertifiedOn)
            .Must(d => d is null || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Validation.CertifiedOnCannotBeFuture");
        RuleFor(c => c.Notes).MaximumLength(500);
    }
}

public class UpdateWorkCenterOperatorValidator : AbstractValidator<UpdateWorkCenterOperatorCommand>
{
    public UpdateWorkCenterOperatorValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.QualificationLevel).IsInEnum();
        RuleFor(c => c.CertifiedOn)
            .Must(d => d is null || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Validation.CertifiedOnCannotBeFuture");
        RuleFor(c => c.Notes).MaximumLength(500);
    }
}
