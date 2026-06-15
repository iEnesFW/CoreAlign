using CoreAlign.Application.GlassEnclosure.Commands;
using FluentValidation;

namespace CoreAlign.Application.GlassEnclosure.Validators;

public class OverrideBomLinePriceCommandValidator : AbstractValidator<OverrideBomLinePriceCommand>
{
    public OverrideBomLinePriceCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.UnitPriceOverride)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.UnitPriceOverride.HasValue);
    }
}

public class AddManualBomLineCommandValidator : AbstractValidator<AddManualBomLineCommand>
{
    public AddManualBomLineCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Data.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Data.Quantity).GreaterThan(0m);
        RuleFor(x => x.Data.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Data.UnitPrice).GreaterThanOrEqualTo(0m);
    }
}

public class DeleteManualBomLineCommandValidator : AbstractValidator<DeleteManualBomLineCommand>
{
    public DeleteManualBomLineCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
    }
}

public class PushBomLinePriceToCatalogCommandValidator : AbstractValidator<PushBomLinePriceToCatalogCommand>
{
    public PushBomLinePriceToCatalogCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
    }
}
