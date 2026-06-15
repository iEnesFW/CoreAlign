using FluentValidation;

namespace CoreAlign.Application.Warranty;

public class CreateWarrantyContractCommandValidator : AbstractValidator<CreateWarrantyContractCommand>
{
    public CreateWarrantyContractCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.WarrantyMonths).GreaterThan(0).LessThanOrEqualTo(360);
        RuleFor(x => x.TermsJson).NotNull();
    }
}

public class ActivateWarrantyContractCommandValidator : AbstractValidator<ActivateWarrantyContractCommand>
{
    public ActivateWarrantyContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ExtendWarrantyContractCommandValidator : AbstractValidator<ExtendWarrantyContractCommand>
{
    public ExtendWarrantyContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.MonthsAdded).GreaterThan(0).LessThanOrEqualTo(120);
    }
}

public class CancelWarrantyContractCommandValidator : AbstractValidator<CancelWarrantyContractCommand>
{
    public CancelWarrantyContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class CreateServiceTicketCommandValidator : AbstractValidator<CreateServiceTicketCommand>
{
    public CreateServiceTicketCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DescriptionMd).NotEmpty().MaximumLength(8000);
    }
}

public class AssignServiceTicketCommandValidator : AbstractValidator<AssignServiceTicketCommand>
{
    public AssignServiceTicketCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class ResolveServiceTicketCommandValidator : AbstractValidator<ResolveServiceTicketCommand>
{
    public ResolveServiceTicketCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ResolutionNotesMd).NotEmpty().MaximumLength(8000);
    }
}

public class CreateMaintenanceScheduleCommandValidator : AbstractValidator<CreateMaintenanceScheduleCommand>
{
    public CreateMaintenanceScheduleCommandValidator()
    {
        RuleFor(x => x.WarrantyContractId).NotEmpty();
        RuleFor(x => x.NextDueDate).GreaterThan(DateTime.UtcNow.AddDays(-1));
    }
}

public class CompleteScheduledMaintenanceCommandValidator : AbstractValidator<CompleteScheduledMaintenanceCommand>
{
    public CompleteScheduledMaintenanceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
