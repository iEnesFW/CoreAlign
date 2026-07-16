using CoreAlign.Application.GlassPlates.Commands;
using CoreAlign.Domain.Enums;
using FluentValidation;

namespace CoreAlign.Application.GlassPlates.Validators;

public class ScrapGlassPlateValidator : AbstractValidator<ScrapGlassPlateCommand>
{
    public ScrapGlassPlateValidator()
    {
        RuleFor(x => x.ReasonCodeId).NotEmpty();
        RuleFor(x => x.PostedByUserId).NotEmpty();
        RuleFor(x => x)
            .Must(c => c.PlateId.HasValue || (c.ProductId.HasValue && c.WarehouseId.HasValue))
            .WithMessage("Either a plate id, or product + warehouse, is required.");
        RuleFor(x => x)
            .Must(c => c.Mode != GlassScrapMode.Count || c.PlateId.HasValue)
            .WithMessage("Count-mode scrap requires a specific plate.");
        RuleFor(x => x.AreaMm2)
            .NotNull().GreaterThan(0m)
            .When(x => x.Mode == GlassScrapMode.Area);
    }
}

public class ReceiveGlassPlatesValidator : AbstractValidator<ReceiveGlassPlatesCommand>
{
    public ReceiveGlassPlatesValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.UnitCostPerM2).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.PostedByUserId).NotEmpty();
        RuleFor(x => x.Plates).NotEmpty();
        RuleForEach(x => x.Plates).ChildRules(line =>
        {
            line.RuleFor(l => l.PlateNumber).NotEmpty().MaximumLength(60);
            line.RuleFor(l => l.WidthMm).GreaterThan(0m);
            line.RuleFor(l => l.HeightMm).GreaterThan(0m);
            line.RuleFor(l => l.ThicknessMm).GreaterThanOrEqualTo(0m);
        });
    }
}

public class AssignUserWarehousesValidator : AbstractValidator<AssignUserWarehousesCommand>
{
    public AssignUserWarehousesValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GrantedByUserId).NotEmpty();
        RuleForEach(x => x.WarehouseIds).NotEmpty();
    }
}

public class SetGlassPlateTrackingValidator : AbstractValidator<SetGlassPlateTrackingCommand>
{
    public SetGlassPlateTrackingValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.MinRemnantAreaMm2).GreaterThanOrEqualTo(0m).When(x => x.MinRemnantAreaMm2.HasValue);
        RuleFor(x => x.MinRemnantWidthMm).GreaterThanOrEqualTo(0m).When(x => x.MinRemnantWidthMm.HasValue);
        RuleFor(x => x.MinRemnantHeightMm).GreaterThanOrEqualTo(0m).When(x => x.MinRemnantHeightMm.HasValue);
        RuleFor(x => x.MinPlateCount).GreaterThanOrEqualTo(0).When(x => x.MinPlateCount.HasValue);
    }
}

public class ConsumeGlassPlateValidator : AbstractValidator<ConsumeGlassPlateCommand>
{
    public ConsumeGlassPlateValidator()
    {
        RuleFor(x => x.PlateId).NotEmpty();
        RuleFor(x => x.CutAreaMm2).GreaterThan(0m);
        RuleFor(x => x.Pieces).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PostedByUserId).NotEmpty();
        RuleFor(x => x.RemnantWidthMm).GreaterThan(0m).When(x => x.RemnantWidthMm.HasValue);
        RuleFor(x => x.RemnantHeightMm).GreaterThan(0m).When(x => x.RemnantHeightMm.HasValue);
    }
}

public class MoveGlassPlateValidator : AbstractValidator<MoveGlassPlateCommand>
{
    public MoveGlassPlateValidator()
    {
        RuleFor(x => x.PlateId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
    }
}
