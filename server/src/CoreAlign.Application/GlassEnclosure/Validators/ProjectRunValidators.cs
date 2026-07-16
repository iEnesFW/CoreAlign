using CoreAlign.Application.GlassEnclosure.Commands;
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
