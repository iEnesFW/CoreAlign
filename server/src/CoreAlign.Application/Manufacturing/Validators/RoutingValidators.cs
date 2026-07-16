using CoreAlign.Application.Manufacturing.Commands;
using FluentValidation;

namespace CoreAlign.Application.Manufacturing.Validators;

public class CreateProductionRoutingValidator : AbstractValidator<CreateProductionRoutingCommand>
{
    public CreateProductionRoutingValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(40)
            .Matches("^[A-Za-z0-9._-]+$").WithMessage("Validation.RoutingCodeFormat");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(1000);
    }
}

public class UpdateProductionRoutingValidator : AbstractValidator<UpdateProductionRoutingCommand>
{
    public UpdateProductionRoutingValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Code).NotEmpty().MaximumLength(40)
            .Matches("^[A-Za-z0-9._-]+$").WithMessage("Validation.RoutingCodeFormat");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(1000);
    }
}

public class SetRoutingStepsValidator : AbstractValidator<SetRoutingStepsCommand>
{
    public SetRoutingStepsValidator()
    {
        RuleFor(c => c.RoutingId).NotEmpty();
        RuleFor(c => c.Steps).NotEmpty();
        RuleFor(c => c.Steps)
            .Must(BeGaplessSequence)
            .WithMessage("Validation.RoutingStepsMustBeGapless")
            .When(c => c.Steps is { Count: > 0 });

        RuleForEach(c => c.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.StepNumber).GreaterThanOrEqualTo(1);
            step.RuleFor(s => s.WorkCenterId).NotEmpty();
            step.RuleFor(s => s.OperationName).NotEmpty().MaximumLength(100);
            step.RuleFor(s => s.OperationType).IsInEnum();
            step.RuleFor(s => s.SetupTimeMinutes).GreaterThanOrEqualTo(0m)
                .WithMessage("Validation.SetupTimeMustBeNonNegative");
            step.RuleFor(s => s.RunTimeMinutesPerUnit).GreaterThanOrEqualTo(0m)
                .WithMessage("Validation.RunTimeMustBeNonNegative");
            step.RuleFor(s => s.RunTimeMinutesPerSqm).GreaterThanOrEqualTo(0m)
                .When(s => s.RunTimeMinutesPerSqm.HasValue);
            step.RuleFor(s => s.ScrapPercentage).InclusiveBetween(0m, 100m);
            step.RuleFor(s => s.Instructions).MaximumLength(2000);
        });
    }

    private static bool BeGaplessSequence(IReadOnlyList<RoutingStepInput> steps)
    {
        var numbers = steps.Select(s => s.StepNumber).OrderBy(n => n).ToList();
        if (numbers.Distinct().Count() != numbers.Count)
        {
            return false;
        }
        for (var i = 0; i < numbers.Count; i++)
        {
            if (numbers[i] != i + 1)
            {
                return false;
            }
        }
        return true;
    }
}

public class AssignRoutingToProductValidator : AbstractValidator<AssignRoutingToProductCommand>
{
    public AssignRoutingToProductValidator()
    {
        RuleFor(c => c.ProductId).NotEmpty();
    }
}
