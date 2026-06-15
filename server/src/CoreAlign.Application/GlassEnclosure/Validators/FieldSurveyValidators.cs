using CoreAlign.Application.GlassEnclosure.Commands;
using FluentValidation;

namespace CoreAlign.Application.GlassEnclosure.Validators;

public class CreateFieldSurveyCommandValidator : AbstractValidator<CreateFieldSurveyCommand>
{
    public CreateFieldSurveyCommandValidator()
    {
        RuleFor(x => x.Data.ProjectId).NotEmpty();
        RuleFor(x => x.Data.GpsLat)
            .InclusiveBetween(-90m, 90m).When(x => x.Data.GpsLat.HasValue);
        RuleFor(x => x.Data.GpsLng)
            .InclusiveBetween(-180m, 180m).When(x => x.Data.GpsLng.HasValue);
        RuleFor(x => x.Data.FloorNumber)
            .InclusiveBetween(-10, 200).When(x => x.Data.FloorNumber.HasValue);
        RuleFor(x => x.Data.BuildingHeightM)
            .InclusiveBetween(0m, 1000m).When(x => x.Data.BuildingHeightM.HasValue);
        RuleFor(x => x.Data.Notes).MaximumLength(2000);
    }
}

public class UpdateFieldSurveyCommandValidator : AbstractValidator<UpdateFieldSurveyCommand>
{
    private const int MaxJsonBytes = 64 * 1024;

    public UpdateFieldSurveyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Data.SlopeTopMm)
            .InclusiveBetween(-500m, 500m).When(x => x.Data.SlopeTopMm.HasValue);
        RuleFor(x => x.Data.SlopeBottomMm)
            .InclusiveBetween(-500m, 500m).When(x => x.Data.SlopeBottomMm.HasValue);
        RuleFor(x => x.Data.SlopeLeftMm)
            .InclusiveBetween(-500m, 500m).When(x => x.Data.SlopeLeftMm.HasValue);
        RuleFor(x => x.Data.SlopeRightMm)
            .InclusiveBetween(-500m, 500m).When(x => x.Data.SlopeRightMm.HasValue);
        RuleFor(x => x.Data.RawMeasurementsJson).NotNull().MaximumLength(MaxJsonBytes).Must(BeValidJson);
        RuleFor(x => x.Data.ObstaclesJson).NotNull().MaximumLength(MaxJsonBytes).Must(BeValidJson);
        RuleFor(x => x.Data.PhotoUrlsJson).NotNull().MaximumLength(MaxJsonBytes).Must(BeValidJson);
        RuleFor(x => x.Data.AnnotatedPhotoUrlsJson).NotNull().MaximumLength(MaxJsonBytes).Must(BeValidJson);
        RuleFor(x => x.Data.Notes).MaximumLength(2000);
    }

    private static bool BeValidJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(value);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}

public class ApproveFieldSurveyCommandValidator : AbstractValidator<ApproveFieldSurveyCommand>
{
    public ApproveFieldSurveyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class RejectFieldSurveyCommandValidator : AbstractValidator<RejectFieldSurveyCommand>
{
    public RejectFieldSurveyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Data.Reason).MaximumLength(1000);
    }
}
