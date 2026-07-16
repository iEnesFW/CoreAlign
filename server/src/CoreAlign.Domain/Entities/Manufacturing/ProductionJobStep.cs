using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Manufacturing;

public class ProductionJobStep : TenantEntity
{
    public Guid ProductionJobId { get; private set; }
    public int StepNumber { get; private set; }
    public Guid? WorkCenterId { get; private set; }
    public Guid? SourceRoutingStepId { get; private set; }
    public string OperationName { get; private set; } = string.Empty;
    public RoutingOperationType OperationType { get; private set; }
    public decimal SetupTimeMinutes { get; private set; }
    public decimal RunTimeMinutesPerUnit { get; private set; }
    public decimal? RunTimeMinutesPerSqm { get; private set; }
    public decimal ScrapPercentage { get; private set; }
    public string? Instructions { get; private set; }
    public bool IsOptional { get; private set; }

    public ProductionJobStepStatus Status { get; private set; } = ProductionJobStepStatus.Pending;
    public decimal InputQuantity { get; private set; }
    public Guid? AssignedOperatorId { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public decimal? ActualSetupMinutes { get; private set; }
    public decimal? ActualRunMinutes { get; private set; }
    public decimal GoodQuantity { get; private set; }
    public decimal ScrappedQuantity { get; private set; }
    public Guid? ScrapReasonCodeId { get; private set; }
    public int? ReworkedFromStepNumber { get; private set; }
    public int ReworkCount { get; private set; }
    public string? Notes { get; private set; }

    protected ProductionJobStep() { }

    internal ProductionJobStep(Guid productionJobId, ProductionJobStepSnapshot snap, decimal inputQuantity)
    {
        if (snap.StepNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(snap), "Step number must be 1 or greater.");
        if (snap.WorkCenterId == Guid.Empty)
            throw new ArgumentException("Work center must be null or a real id, never Guid.Empty.", nameof(snap));

        ProductionJobId = productionJobId;
        StepNumber = snap.StepNumber;
        WorkCenterId = snap.WorkCenterId;
        SourceRoutingStepId = snap.SourceRoutingStepId;
        OperationName = snap.OperationName.Trim();
        OperationType = snap.OperationType;
        SetupTimeMinutes = snap.SetupTimeMinutes;
        RunTimeMinutesPerUnit = snap.RunTimeMinutesPerUnit;
        RunTimeMinutesPerSqm = snap.RunTimeMinutesPerSqm;
        ScrapPercentage = snap.ScrapPercentage;
        Instructions = snap.Instructions;
        IsOptional = snap.IsOptional;
        Status = ProductionJobStepStatus.Pending;
        InputQuantity = inputQuantity < 0m ? 0m : inputQuantity;
    }

    internal void SetInputQuantity(decimal qty) => InputQuantity = qty < 0m ? 0m : qty;

    internal void Start(Guid operatorId, DateTime utcNow)
    {
        if (Status is not (ProductionJobStepStatus.Pending or ProductionJobStepStatus.Reopened or ProductionJobStepStatus.Skipped))
        {
            throw new InvalidProductionJobStepTransitionException(Status.ToString(), ProductionJobStepStatus.InProgress.ToString());
        }
        Status = ProductionJobStepStatus.InProgress;
        AssignedOperatorId = operatorId;
        StartedAtUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        Touch();
    }

    internal void Finish(
        decimal good,
        decimal scrapped,
        Guid? scrapReasonCodeId,
        decimal? actualSetupMinutes,
        decimal? actualRunMinutes,
        Guid operatorId,
        DateTime utcNow)
    {
        if (Status != ProductionJobStepStatus.InProgress)
        {
            throw new InvalidProductionJobStepTransitionException(Status.ToString(), ProductionJobStepStatus.Completed.ToString());
        }
        if (good < 0m) throw new ArgumentOutOfRangeException(nameof(good), "Good quantity cannot be negative.");
        if (scrapped < 0m) throw new ArgumentOutOfRangeException(nameof(scrapped), "Scrapped quantity cannot be negative.");
        if (actualSetupMinutes is < 0m) throw new ArgumentOutOfRangeException(nameof(actualSetupMinutes));
        if (actualRunMinutes is < 0m) throw new ArgumentOutOfRangeException(nameof(actualRunMinutes));

        Status = ProductionJobStepStatus.Completed;
        GoodQuantity = good;
        ScrappedQuantity = scrapped;
        ScrapReasonCodeId = scrapReasonCodeId;
        ActualSetupMinutes = actualSetupMinutes;
        ActualRunMinutes = actualRunMinutes;
        AssignedOperatorId = operatorId;
        FinishedAtUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        Touch();
    }

    internal void Skip(DateTime utcNow)
    {
        if (Status != ProductionJobStepStatus.Pending)
        {
            throw new InvalidProductionJobStepTransitionException(Status.ToString(), ProductionJobStepStatus.Skipped.ToString());
        }
        Status = ProductionJobStepStatus.Skipped;
        Touch();
    }

    internal void ReopenForRework(int fromStepNumber, string reason, DateTime utcNow)
    {
        if (Status != ProductionJobStepStatus.Completed)
        {
            throw new InvalidProductionJobStepTransitionException(Status.ToString(), ProductionJobStepStatus.Reopened.ToString());
        }
        Status = ProductionJobStepStatus.Reopened;
        ReworkCount++;
        ReworkedFromStepNumber = fromStepNumber;
        FinishedAtUtc = null;
        ActualSetupMinutes = null;
        ActualRunMinutes = null;
        GoodQuantity = 0m;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : reason.Trim();
        Touch();
    }

    internal void ReopenToPending()
    {
        Status = ProductionJobStepStatus.Pending;
        FinishedAtUtc = null;
        ActualSetupMinutes = null;
        ActualRunMinutes = null;
        GoodQuantity = 0m;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
