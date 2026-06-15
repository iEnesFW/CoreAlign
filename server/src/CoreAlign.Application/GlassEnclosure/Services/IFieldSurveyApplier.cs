using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record FieldSurveyApplyResult(
    Guid ProjectId,
    Guid SurveyId,
    int RunsUpdated,
    decimal MaxSlopeAdjustmentMm,
    int ToleranceTopMm,
    int ToleranceSideMm);

public interface IFieldSurveyApplier
{
    Task<FieldSurveyApplyResult> ApplyAsync(GlassProject project, FieldSurvey survey, CancellationToken cancellationToken = default);
}

public class FieldSurveyApplier : IFieldSurveyApplier
{
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public FieldSurveyApplier(IGlassEnclosureSettingsRepository settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }

    public async Task<FieldSurveyApplyResult> ApplyAsync(GlassProject project, FieldSurvey survey, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);
        var toleranceTopMm = settings.FieldToleranceTopMm;
        var toleranceSideMm = settings.FieldToleranceSideMm;

        var slopeTop = Math.Abs(survey.SlopeTopMm ?? 0m);
        var slopeBottom = Math.Abs(survey.SlopeBottomMm ?? 0m);
        var slopeMax = Math.Max(slopeTop, slopeBottom);
        var slopeSide = Math.Max(Math.Abs(survey.SlopeLeftMm ?? 0m), Math.Abs(survey.SlopeRightMm ?? 0m));

        var runsUpdated = 0;
        foreach (var run in project.Runs)
        {
            var adjustedLengthMm = Math.Max(100, run.LengthMm - 2 * toleranceSideMm - (int)Math.Ceiling(slopeSide));
            var adjustedHeightMm = Math.Max(100, run.HeightMm - toleranceTopMm - (int)Math.Ceiling(slopeMax));
            run.UpdateGeometry(
                lengthMm: adjustedLengthMm,
                heightMm: adjustedHeightMm,
                originX: run.OriginX,
                originY: run.OriginY,
                rotationDeg: run.RotationDeg);
            runsUpdated += 1;
        }

        return new FieldSurveyApplyResult(
            project.Id, survey.Id, runsUpdated,
            decimal.Round(Math.Max(slopeMax, slopeSide), 2),
            toleranceTopMm,
            toleranceSideMm);
    }
}
