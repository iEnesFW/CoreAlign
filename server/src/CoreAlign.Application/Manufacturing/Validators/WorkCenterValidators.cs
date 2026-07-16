using CoreAlign.Application.Manufacturing.Commands;
using FluentValidation;

namespace CoreAlign.Application.Manufacturing.Validators;

public class CreateWorkCenterValidator : AbstractValidator<CreateWorkCenterCommand>
{
    public CreateWorkCenterValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.DailyCapacityMinutes).GreaterThanOrEqualTo(0m)
            .WithMessage("Validation.CapacityMustBeNonNegative");
    }
}

public class UpdateWorkCenterValidator : AbstractValidator<UpdateWorkCenterCommand>
{
    public UpdateWorkCenterValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Code).NotEmpty().MaximumLength(40);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.DailyCapacityMinutes).GreaterThanOrEqualTo(0m)
            .WithMessage("Validation.CapacityMustBeNonNegative");
    }
}
