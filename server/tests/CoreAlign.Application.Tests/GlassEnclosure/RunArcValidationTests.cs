using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Validators;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class RunArcValidationTests
{
    private static UpdateRunDto Dto(int lengthMm, int? radiusMm, decimal? sweepDeg) =>
        new(lengthMm, 2400, 0m, 0m, 0m, "R", Guid.NewGuid(), null, false, false, null,
            GeomArcRadiusMm: radiusMm, GeomArcSweepDeg: sweepDeg);

    private static AddRunDto AddDto(int lengthMm, int? radiusMm, decimal? sweepDeg) =>
        new(lengthMm, 2400, Guid.NewGuid(), 0m, 0m, 0m, "R", null, false, false, null,
            GeomArcRadiusMm: radiusMm, GeomArcSweepDeg: sweepDeg);

    [Fact]
    public void A_constructible_arc_triple_passes()
    {
        // chord = 2·2000·sin(45°) ≈ 2828 for R2000 / 90°.
        var result = new UpdateRunCommandValidator()
            .Validate(new UpdateRunCommand(Guid.NewGuid(), Guid.NewGuid(), Dto(2828, 2000, 90m)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void An_impossible_arc_triple_is_refused()
    {
        // A 3000 chord cannot belong to R2000 / 90° (true chord ≈ 2828) — the read side would
        // silently mis-measure the developed glass.
        var result = new UpdateRunCommandValidator()
            .Validate(new UpdateRunCommand(Guid.NewGuid(), Guid.NewGuid(), Dto(3000, 2000, 90m)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Validation.RunArcTripleInconsistent");
    }

    [Fact]
    public void AddRun_refuses_the_same_impossible_triple()
    {
        var result = new AddRunCommandValidator()
            .Validate(new AddRunCommand(Guid.NewGuid(), AddDto(3000, 2000, 90m)));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void A_legacy_half_arc_with_radius_but_no_sweep_stays_accepted()
    {
        var result = new UpdateRunCommandValidator()
            .Validate(new UpdateRunCommand(Guid.NewGuid(), Guid.NewGuid(), Dto(3000, 2000, null)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void A_straight_run_is_untouched()
    {
        var result = new UpdateRunCommandValidator()
            .Validate(new UpdateRunCommand(Guid.NewGuid(), Guid.NewGuid(), Dto(3000, null, null)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void The_tolerance_scales_with_the_radius_for_sweep_quantisation()
    {
        // R20000 / 10° → chord ≈ 3486.7; the client's 0.1° sweep quantum alone moves the chord by
        // ~r·0.00087 ≈ 17 mm, so a few-mm drift on a big radius must not be refused.
        var result = new UpdateRunCommandValidator()
            .Validate(new UpdateRunCommand(Guid.NewGuid(), Guid.NewGuid(), Dto(3480, 20000, 10m)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void A_negative_sweep_uses_its_magnitude()
    {
        var result = new UpdateRunCommandValidator()
            .Validate(new UpdateRunCommand(Guid.NewGuid(), Guid.NewGuid(), Dto(2828, 2000, -90m)));
        result.IsValid.Should().BeTrue();
    }
}
