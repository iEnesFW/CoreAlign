using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Application.GlassEnclosure.DTOs;
using FluentValidation;

namespace CoreAlign.Application.GlassEnclosure.Validators;

public class AddRunCommandValidator : AbstractValidator<AddRunCommand>
{
    public AddRunCommandValidator()
    {
        RuleFor(x => x.Data.GeomTiltDeg)
            .InclusiveBetween(-90m, 90m).When(x => x.Data.GeomTiltDeg.HasValue);
        RuleFor(x => x.Data.GeomArcRadiusMm)
            .GreaterThanOrEqualTo(100).When(x => x.Data.GeomArcRadiusMm.HasValue);
        RuleFor(x => x.Data.GeomArcSweepDeg)
            .InclusiveBetween(-360m, 360m).When(x => x.Data.GeomArcSweepDeg.HasValue);
    }
}

public class UpdateRunCommandValidator : AbstractValidator<UpdateRunCommand>
{
    public UpdateRunCommandValidator()
    {
        RuleFor(x => x.Data.GeomTiltDeg)
            .InclusiveBetween(-90m, 90m).When(x => x.Data.GeomTiltDeg.HasValue);
        RuleFor(x => x.Data.GeomArcRadiusMm)
            .GreaterThanOrEqualTo(100).When(x => x.Data.GeomArcRadiusMm.HasValue);
        RuleFor(x => x.Data.GeomArcSweepDeg)
            .InclusiveBetween(-360m, 360m).When(x => x.Data.GeomArcSweepDeg.HasValue);
    }
}

public class AddPanelCommandValidator : AbstractValidator<AddPanelCommand>
{
    public AddPanelCommandValidator()
    {
        RuleFor(x => x.Data.ShapePointsJson)
            .Must((cmd, json) => PanelShapeValidation.IsValid(cmd.Data.ShapeKind, json, cmd.Data.WidthMm, cmd.Data.HeightMm))
            .WithMessage("Validation.PanelShapeOutlineInvalid");
    }
}

public class UpdatePanelCommandValidator : AbstractValidator<UpdatePanelCommand>
{
    public UpdatePanelCommandValidator()
    {
        RuleFor(x => x.Data.ShapePointsJson)
            .Must((cmd, json) => PanelShapeValidation.IsValid(cmd.Data.ShapeKind, json, cmd.Data.WidthMm, cmd.Data.HeightMm))
            .WithMessage("Validation.PanelShapeOutlineInvalid");
    }
}

/// <summary>
/// The designer normalises a shaped pane's outline before it ships, but the API is callable by
/// anything — and a self-intersecting outline silently under-reports the silhouette area that the
/// BOM prices and the cut list orders (its shoelace lobes cancel). Reject it at the boundary.
/// </summary>
internal static class PanelShapeValidation
{
    public static bool IsValid(string? shapeKind, string? json, int widthMm, int? heightMm)
    {
        if (shapeKind != "polygon") return true;
        if (string.IsNullOrWhiteSpace(json)) return true;
        return PanelShapeGeometry.CheckPolygonJson(json, widthMm, heightMm).IsValid;
    }
}

public class SetRunPanelsCommandValidator : AbstractValidator<SetRunPanelsCommand>
{
    public SetRunPanelsCommandValidator()
    {
        RuleForEach(x => x.Data.Panels).ChildRules(panel =>
        {
            panel.RuleFor(p => p.WidthMm)
                .GreaterThan(0).WithMessage("Validation.PanelWidthMustBePositive");
            panel.RuleFor(p => p.GlassTypeId)
                .NotEmpty().WithMessage("Validation.PanelGlassTypeRequired");
        });
        RuleFor(x => x.Data.Panels)
            .Must(NoDuplicateNonEmptyIds).WithMessage("Validation.DuplicatePanelId");
    }

    private static bool NoDuplicateNonEmptyIds(IReadOnlyList<PanelSpecDto> panels)
    {
        var seen = new HashSet<Guid>();
        foreach (var panel in panels)
        {
            if (panel.Id == Guid.Empty) continue;
            if (!seen.Add(panel.Id)) return false;
        }
        return true;
    }
}
