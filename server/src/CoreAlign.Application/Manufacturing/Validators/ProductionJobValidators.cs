using CoreAlign.Application.Manufacturing.Commands;
using FluentValidation;

namespace CoreAlign.Application.Manufacturing.Validators;

public class CreateProductionJobValidator : AbstractValidator<CreateProductionJobCommand>
{
    public CreateProductionJobValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.PlannedQuantity).GreaterThan(0m);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(20);
    }
}

public class ReleaseProductionJobValidator : AbstractValidator<ReleaseProductionJobCommand>
{
    public ReleaseProductionJobValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
    }
}

public class StartJobStepValidator : AbstractValidator<StartJobStepCommand>
{
    public StartJobStepValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.StepNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.OperatorId).NotEmpty();
    }
}

public class FinishJobStepValidator : AbstractValidator<FinishJobStepCommand>
{
    public FinishJobStepValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.StepNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.GoodQuantity).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.ScrappedQuantity).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.OperatorId).NotEmpty();
        RuleFor(x => x.ActualSetupMinutes).GreaterThanOrEqualTo(0m).When(x => x.ActualSetupMinutes.HasValue);
        RuleFor(x => x.ActualRunMinutes).GreaterThanOrEqualTo(0m).When(x => x.ActualRunMinutes.HasValue);
    }
}

public class SkipJobStepValidator : AbstractValidator<SkipJobStepCommand>
{
    public SkipJobStepValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.StepNumber).GreaterThanOrEqualTo(1);
    }
}

public class ReworkToStepValidator : AbstractValidator<ReworkToStepCommand>
{
    public ReworkToStepValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.TargetStepNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.FromStepNumber).GreaterThan(x => x.TargetStepNumber);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class PutJobOnHoldValidator : AbstractValidator<PutJobOnHoldCommand>
{
    public PutJobOnHoldValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ResumeJobValidator : AbstractValidator<ResumeJobCommand>
{
    public ResumeJobValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CancelProductionJobValidator : AbstractValidator<CancelProductionJobCommand>
{
    public CancelProductionJobValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class CompleteProductionJobValidator : AbstractValidator<CompleteProductionJobCommand>
{
    public CompleteProductionJobValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CompletedQuantity).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.WarehouseId).NotEmpty();
    }
}
