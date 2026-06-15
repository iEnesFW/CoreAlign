using CoreAlign.Application.GlassEnclosure.Commands;
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
