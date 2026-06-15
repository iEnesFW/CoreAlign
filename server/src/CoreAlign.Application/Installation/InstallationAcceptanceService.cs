using System.Text.Json;
using System.Text.Json.Nodes;
using CoreAlign.Application.Installation.Templates;
using CoreAlign.Domain.Entities.Installation;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Installation;

public sealed class InstallationAcceptanceService : IInstallationAcceptanceService
{
    private readonly IInstallationAcceptanceRepository _acceptances;
    private readonly IPunchListRepository _punchList;
    private readonly IGlassWorkOrderRepository _workOrders;
    private readonly IGlassProjectRepository _projects;

    public InstallationAcceptanceService(
        IInstallationAcceptanceRepository acceptances,
        IPunchListRepository punchList,
        IGlassWorkOrderRepository workOrders,
        IGlassProjectRepository projects)
    {
        _acceptances = acceptances;
        _punchList = punchList;
        _workOrders = workOrders;
        _projects = projects;
    }

    public async Task<InstallationAcceptance> StartAsync(Guid workOrderId, Guid inspectorUserId, CancellationToken cancellationToken = default)
    {
        var existing = await _acceptances.GetByWorkOrderIdAsync(workOrderId, cancellationToken);
        if (existing is not null) return existing;

        var workOrder = await _workOrders.GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"WorkOrder {workOrderId} not found.");
        var project = await _projects.GetByIdAsync(workOrder.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {workOrder.ProjectId} not found.");

        var acceptance = new InstallationAcceptance(
            workOrderId: workOrderId,
            projectId: workOrder.ProjectId,
            customerId: project.CustomerId,
            inspectorUserId: inspectorUserId,
            initialChecklistJson: StandardChecklist.BuildInitialChecklistJson());

        await _acceptances.AddAsync(acceptance, cancellationToken);
        return acceptance;
    }

    public async Task UpdateChecklistAsync(Guid acceptanceId, string category, string itemKey, InstallationChecklistResult result, string? notes, CancellationToken cancellationToken = default)
    {
        var acceptance = await _acceptances.GetByIdAsync(acceptanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Acceptance {acceptanceId} not found.");

        var node = JsonNode.Parse(acceptance.ChecklistJson) as JsonArray
            ?? new JsonArray();

        foreach (var catNode in node)
        {
            if (catNode is null) continue;
            if (!string.Equals((string?)catNode["category"], category, StringComparison.Ordinal)) continue;

            if (catNode["items"] is JsonArray items)
            {
                foreach (var item in items)
                {
                    if (item is null) continue;
                    if (!string.Equals((string?)item["key"], itemKey, StringComparison.Ordinal)) continue;
                    item["result"] = result.ToString();
                    item["notes"] = notes;
                }
            }
        }

        acceptance.UpdateChecklist(node.ToJsonString());
        _acceptances.Update(acceptance);
    }

    public async Task AddPhotoAsync(Guid acceptanceId, Guid fileId, CancellationToken cancellationToken = default)
    {
        var acceptance = await _acceptances.GetByIdAsync(acceptanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Acceptance {acceptanceId} not found.");

        var photos = JsonSerializer.Deserialize<List<Guid>>(acceptance.PhotoFileIds) ?? new List<Guid>();
        if (!photos.Contains(fileId)) photos.Add(fileId);
        acceptance.AddPhoto(JsonSerializer.Serialize(photos));
        _acceptances.Update(acceptance);
    }

    public async Task CaptureSignatureAsync(Guid acceptanceId, Guid fileId, string customerName, CancellationToken cancellationToken = default)
    {
        var acceptance = await _acceptances.GetByIdAsync(acceptanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Acceptance {acceptanceId} not found.");
        acceptance.CaptureSignature(fileId, customerName);
        _acceptances.Update(acceptance);
    }

    public async Task AcceptAsync(Guid acceptanceId, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var duplicate = await _acceptances.GetByAcceptIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (duplicate is not null && duplicate.Id != acceptanceId)
            {
                return;
            }
        }

        var acceptance = await _acceptances.GetByIdAsync(acceptanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Acceptance {acceptanceId} not found.");

        if (acceptance.Status == InstallationAcceptanceStatus.Accepted)
        {
            return;
        }

        acceptance.MarkAccepted(idempotencyKey);
        _acceptances.Update(acceptance);
    }

    public async Task RejectAsync(Guid acceptanceId, string reason, CancellationToken cancellationToken = default)
    {
        var acceptance = await _acceptances.GetByIdAsync(acceptanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Acceptance {acceptanceId} not found.");
        acceptance.MarkRejected(reason);
        _acceptances.Update(acceptance);
    }

    public async Task<PunchListItem> AddPunchListAsync(Guid acceptanceId, string description, PunchListSeverity severity, CancellationToken cancellationToken = default)
    {
        var acceptance = await _acceptances.GetByIdAsync(acceptanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Acceptance {acceptanceId} not found.");

        var item = new PunchListItem(acceptance.Id, description, severity);
        await _punchList.AddAsync(item, cancellationToken);
        return item;
    }

    public async Task ResolvePunchListAsync(Guid punchItemId, string? resolutionNotes, CancellationToken cancellationToken = default)
    {
        var item = await _punchList.GetByIdAsync(punchItemId, cancellationToken)
            ?? throw new InvalidOperationException($"PunchListItem {punchItemId} not found.");
        item.Resolve(resolutionNotes);
        _punchList.Update(item);
    }
}
