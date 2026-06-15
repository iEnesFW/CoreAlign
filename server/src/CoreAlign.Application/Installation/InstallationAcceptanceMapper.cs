using CoreAlign.Domain.Entities.Installation;

namespace CoreAlign.Application.Installation;

internal static class InstallationAcceptanceMapper
{
    public static InstallationAcceptanceDto ToDto(InstallationAcceptance a) => new(
        a.Id,
        a.WorkOrderId,
        a.ProjectId,
        a.CustomerId,
        a.Status,
        a.StartedAtUtc,
        a.CompletedAtUtc,
        a.InspectorUserId,
        a.CustomerSignatureFileId,
        a.CustomerSignatureCapturedAtUtc,
        a.CustomerName,
        a.ChecklistJson,
        a.PhotoFileIds,
        a.NotesMd,
        a.RejectionReason);

    public static PunchListItemDto ToDto(PunchListItem p) => new(
        p.Id,
        p.AcceptanceId,
        p.Description,
        p.Severity,
        p.Status,
        p.AssignedToUserId,
        p.ResolvedAtUtc,
        p.ResolutionNotes);
}
