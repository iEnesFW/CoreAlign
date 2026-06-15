using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class FieldSurvey : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        ((ISoftDeletable)this).MarkDeletedInternal(userId, reason, utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        ((ISoftDeletable)this).RestoreInternal();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid ProjectId { get; private set; }
    public Guid SurveyedByUserId { get; private set; }
    public DateTime SurveyedAtUtc { get; private set; } = DateTime.UtcNow;
    public decimal? GpsLat { get; private set; }
    public decimal? GpsLng { get; private set; }
    public int? FloorNumber { get; private set; }
    public decimal? BuildingHeightM { get; private set; }
    public decimal? SlopeTopMm { get; private set; }
    public decimal? SlopeBottomMm { get; private set; }
    public decimal? SlopeLeftMm { get; private set; }
    public decimal? SlopeRightMm { get; private set; }
    public string RawMeasurementsJson { get; private set; } = "[]";
    public string ObstaclesJson { get; private set; } = "[]";
    public string PhotoUrlsJson { get; private set; } = "[]";
    public string AnnotatedPhotoUrlsJson { get; private set; } = "[]";
    public FieldSurveyStatus Status { get; private set; } = FieldSurveyStatus.InProgress;
    public DateTime? AppliedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected FieldSurvey() { }

    public FieldSurvey(
        Guid projectId,
        Guid surveyedByUserId,
        decimal? gpsLat = null,
        decimal? gpsLng = null,
        int? floorNumber = null,
        decimal? buildingHeightM = null,
        string? notes = null)
    {
        ProjectId = projectId;
        SurveyedByUserId = surveyedByUserId;
        GpsLat = gpsLat;
        GpsLng = gpsLng;
        FloorNumber = floorNumber;
        BuildingHeightM = buildingHeightM;
        Notes = notes;
    }

    public void UpdateMeasurements(
        decimal? slopeTopMm,
        decimal? slopeBottomMm,
        decimal? slopeLeftMm,
        decimal? slopeRightMm,
        string rawMeasurementsJson,
        string obstaclesJson,
        string photoUrlsJson,
        string annotatedPhotoUrlsJson,
        string? notes)
    {
        SlopeTopMm = slopeTopMm;
        SlopeBottomMm = slopeBottomMm;
        SlopeLeftMm = slopeLeftMm;
        SlopeRightMm = slopeRightMm;
        RawMeasurementsJson = rawMeasurementsJson;
        ObstaclesJson = obstaclesJson;
        PhotoUrlsJson = photoUrlsJson;
        AnnotatedPhotoUrlsJson = annotatedPhotoUrlsJson;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Submit()
    {
        Status = FieldSurveyStatus.Submitted;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new GlassFieldSurveySubmittedEvent(TenantId, Id, ProjectId, SurveyedByUserId, DateTime.UtcNow));
    }

    public void Approve()
    {
        Status = FieldSurveyStatus.Approved;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(string? reason)
    {
        Status = FieldSurveyStatus.Rejected;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : $"{Notes}\nRejection: {reason}".Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkApplied()
    {
        AppliedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = AppliedAtUtc.Value;
    }
}
