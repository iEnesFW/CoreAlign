using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Manufacturing;

public class WorkCenterOperator : TenantEntity
{
    public Guid WorkCenterId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public OperatorQualificationLevel QualificationLevel { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateOnly? CertifiedOn { get; private set; }
    public string? Notes { get; private set; }
    public string? PinCode { get; private set; }

    protected WorkCenterOperator() { }

    public WorkCenterOperator(
        Guid workCenterId,
        Guid employeeId,
        OperatorQualificationLevel level,
        bool isPrimary = false,
        DateOnly? certifiedOn = null,
        string? notes = null,
        string? pinCode = null)
    {
        if (workCenterId == Guid.Empty)
            throw new ArgumentException("Work center is required.", nameof(workCenterId));
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee is required.", nameof(employeeId));

        WorkCenterId = workCenterId;
        EmployeeId = employeeId;
        QualificationLevel = level;
        IsPrimary = isPrimary;
        IsActive = true;
        CertifiedOn = certifiedOn;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        PinCode = string.IsNullOrWhiteSpace(pinCode) ? null : pinCode.Trim();
    }

    public void Update(
        OperatorQualificationLevel level,
        bool isPrimary,
        bool isActive,
        DateOnly? certifiedOn,
        string? notes,
        string? pinCode)
    {
        QualificationLevel = level;
        IsActive = isActive;
        IsPrimary = isActive && isPrimary;
        CertifiedOn = certifiedOn;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        PinCode = string.IsNullOrWhiteSpace(pinCode) ? null : pinCode.Trim();
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        IsPrimary = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
