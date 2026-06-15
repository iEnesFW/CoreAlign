using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Warranty;

public class MaintenanceSchedule : TenantEntity
{
    public Guid WarrantyContractId { get; private set; }
    public MaintenanceScheduleType Type { get; private set; }
    public DateTime NextDueDate { get; private set; }
    public DateTime? LastCompletedAtUtc { get; private set; }
    public string RecurrencePattern { get; private set; } = "0 0 1 1 *";
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    protected MaintenanceSchedule() { }

    public MaintenanceSchedule(
        Guid warrantyContractId,
        MaintenanceScheduleType type,
        DateTime nextDueDate,
        string recurrencePattern,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(recurrencePattern))
            throw new ArgumentException("RecurrencePattern is required.", nameof(recurrencePattern));

        WarrantyContractId = warrantyContractId;
        Type = type;
        NextDueDate = nextDueDate;
        RecurrencePattern = recurrencePattern.Trim();
        Notes = notes;
        IsActive = true;
    }

    public void CompleteOccurrence(DateTime completedAtUtc, TimeSpan recurrence)
    {
        LastCompletedAtUtc = completedAtUtc;
        NextDueDate = completedAtUtc.Add(recurrence);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reschedule(DateTime nextDueDate)
    {
        NextDueDate = nextDueDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
