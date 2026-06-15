using FluentValidation;

namespace CoreAlign.Application.Mrp;

public class RunMrpPreviewQueryValidator : AbstractValidator<RunMrpPreviewQuery>
{
    public RunMrpPreviewQueryValidator()
    {
        RuleFor(x => x.HorizonDays).InclusiveBetween(1, 365);
    }
}

public class GetMrpItemPlanQueryValidator : AbstractValidator<GetMrpItemPlanQuery>
{
    public GetMrpItemPlanQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.HorizonDays).InclusiveBetween(1, 365);
    }
}

public class ListMrpActionMessagesQueryValidator : AbstractValidator<ListMrpActionMessagesQuery>
{
    public ListMrpActionMessagesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class ListMrpPlanRunsQueryValidator : AbstractValidator<ListMrpPlanRunsQuery>
{
    public ListMrpPlanRunsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class GetMrpPeggingQueryValidator : AbstractValidator<GetMrpPeggingQuery>
{
    public GetMrpPeggingQueryValidator()
    {
        RuleFor(x => x.PlanRunId).NotEmpty();
        RuleFor(x => x.ComponentProductId).NotEmpty();
    }
}

public class CommitMrpPlanCommandValidator : AbstractValidator<CommitMrpPlanCommand>
{
    public CommitMrpPlanCommandValidator()
    {
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.HorizonDays).InclusiveBetween(1, 365);
    }
}

public class ReleasePlannedOrdersCommandValidator : AbstractValidator<ReleasePlannedOrdersCommand>
{
    public ReleasePlannedOrdersCommandValidator()
    {
        RuleFor(x => x.PlanRunId).NotEmpty();
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.PlannedOrderIds).NotEmpty();
        RuleForEach(x => x.PlannedOrderIds).NotEmpty();
    }
}

public class FirmMrpPlannedOrderCommandValidator : AbstractValidator<FirmMrpPlannedOrderCommand>
{
    public FirmMrpPlannedOrderCommandValidator()
    {
        RuleFor(x => x.PlannedOrderId).NotEmpty();
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.OverrideQuantity)
            .GreaterThan(0m)
            .When(x => x.OverrideQuantity.HasValue);
    }
}

public class DismissMrpActionMessageCommandValidator : AbstractValidator<DismissMrpActionMessageCommand>
{
    public DismissMrpActionMessageCommandValidator()
    {
        RuleFor(x => x.ActionMessageId).NotEmpty();
    }
}
