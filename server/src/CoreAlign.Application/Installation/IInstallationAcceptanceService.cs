using CoreAlign.Domain.Entities.Installation;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Installation;

public interface IInstallationAcceptanceService
{
    Task<InstallationAcceptance> StartAsync(Guid workOrderId, Guid inspectorUserId, CancellationToken cancellationToken = default);
    Task UpdateChecklistAsync(Guid acceptanceId, string category, string itemKey, InstallationChecklistResult result, string? notes, CancellationToken cancellationToken = default);
    Task AddPhotoAsync(Guid acceptanceId, Guid fileId, CancellationToken cancellationToken = default);
    Task CaptureSignatureAsync(Guid acceptanceId, Guid fileId, string customerName, CancellationToken cancellationToken = default);
    Task AcceptAsync(Guid acceptanceId, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid acceptanceId, string reason, CancellationToken cancellationToken = default);
    Task<PunchListItem> AddPunchListAsync(Guid acceptanceId, string description, PunchListSeverity severity, CancellationToken cancellationToken = default);
    Task ResolvePunchListAsync(Guid punchItemId, string? resolutionNotes, CancellationToken cancellationToken = default);
}
