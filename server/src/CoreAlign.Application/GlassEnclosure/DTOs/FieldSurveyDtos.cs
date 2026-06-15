using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.DTOs;

public record FieldSurveyDto(
    Guid Id,
    Guid ProjectId,
    Guid SurveyedByUserId,
    DateTime SurveyedAtUtc,
    decimal? GpsLat,
    decimal? GpsLng,
    int? FloorNumber,
    decimal? BuildingHeightM,
    decimal? SlopeTopMm,
    decimal? SlopeBottomMm,
    decimal? SlopeLeftMm,
    decimal? SlopeRightMm,
    string RawMeasurementsJson,
    string ObstaclesJson,
    string PhotoUrlsJson,
    string AnnotatedPhotoUrlsJson,
    FieldSurveyStatus Status,
    DateTime? AppliedAtUtc,
    string? Notes);

public record CreateFieldSurveyDto(
    Guid ProjectId,
    decimal? GpsLat,
    decimal? GpsLng,
    int? FloorNumber,
    decimal? BuildingHeightM,
    string? Notes);

public record UpdateFieldSurveyDto(
    decimal? SlopeTopMm,
    decimal? SlopeBottomMm,
    decimal? SlopeLeftMm,
    decimal? SlopeRightMm,
    string RawMeasurementsJson,
    string ObstaclesJson,
    string PhotoUrlsJson,
    string AnnotatedPhotoUrlsJson,
    string? Notes);

public record ApproveFieldSurveyDto(bool ApplyToProject);

public record RejectFieldSurveyDto(string? Reason);

public record FieldSurveyApplyResultDto(
    Guid ProjectId,
    Guid SurveyId,
    int RunsUpdated,
    decimal MaxSlopeAdjustmentMm,
    int ToleranceTopMm,
    int ToleranceSideMm);

public record FieldSurveyUploadResultDto(string Url, string ContentType, long SizeBytes);
