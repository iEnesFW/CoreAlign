using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Manufacturing;

public record ProductionJobStepSnapshot(
    int StepNumber,
    Guid? WorkCenterId,
    Guid? SourceRoutingStepId,
    string OperationName,
    RoutingOperationType OperationType,
    decimal SetupTimeMinutes,
    decimal RunTimeMinutesPerUnit,
    decimal? RunTimeMinutesPerSqm,
    decimal ScrapPercentage,
    string? Instructions,
    bool IsOptional);

public class ProductionJob : TenantEntity, IHasConcurrencyToken
{
    public string JobNumber { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }
    public decimal PlannedQuantity { get; private set; }
    public decimal CompletedQuantity { get; private set; }
    public decimal ScrappedQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public ProductionJobStatus Status { get; private set; } = ProductionJobStatus.Draft;

    public Guid? SourcePlannedProductionOrderId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? SourceRoutingId { get; private set; }
    public string? RoutingCodeSnapshot { get; private set; }
    public string? RoutingNameSnapshot { get; private set; }
    public long? RoutingSnapshotVersion { get; private set; }
    public int? CurrentStepNumber { get; private set; }

    public DateTime? PlannedStartDateUtc { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? Notes { get; private set; }

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    private readonly List<ProductionJobStep> _steps = new();
    public IReadOnlyCollection<ProductionJobStep> Steps => _steps.AsReadOnly();

    private readonly List<ProductionJobLog> _logs = new();
    public IReadOnlyCollection<ProductionJobLog> Logs => _logs.AsReadOnly();

    public bool IsTerminal => Status is ProductionJobStatus.Completed or ProductionJobStatus.Cancelled;
    public bool AllRequiredStepsDone =>
        _steps.Count > 0
        && _steps.All(s => s.Status is ProductionJobStepStatus.Completed or ProductionJobStepStatus.Skipped);
    public ProductionJobStep? CurrentStep => _steps.FirstOrDefault(s => s.StepNumber == CurrentStepNumber);

    protected ProductionJob() { }

    public ProductionJob(
        string jobNumber,
        Guid productId,
        decimal plannedQuantity,
        string unitOfMeasure,
        Guid? sourcePlannedProductionOrderId,
        Guid? warehouseId,
        DateTime? plannedStartDateUtc,
        DateTime? dueDateUtc,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(jobNumber))
            throw new ArgumentException("Job number is required.", nameof(jobNumber));
        if (plannedQuantity <= 0m)
            throw new ArgumentOutOfRangeException(nameof(plannedQuantity), "Planned quantity must be positive.");

        JobNumber = jobNumber.Trim();
        ProductId = productId;
        PlannedQuantity = plannedQuantity;
        UnitOfMeasure = (unitOfMeasure ?? string.Empty).Trim();
        SourcePlannedProductionOrderId = sourcePlannedProductionOrderId;
        WarehouseId = warehouseId;
        PlannedStartDateUtc = AsUtc(plannedStartDateUtc);
        DueDateUtc = AsUtc(dueDateUtc);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Status = ProductionJobStatus.Draft;
    }

    public void SnapshotRouting(
        Guid? sourceRoutingId,
        string? routingCode,
        string? routingName,
        long? routingVersion,
        IReadOnlyList<ProductionJobStepSnapshot> steps)
    {
        if (Status != ProductionJobStatus.Draft)
        {
            throw new ProductionJobNotEditableException();
        }
        if (steps is null || steps.Count == 0)
        {
            throw new ProductionJobHasNoStepsException();
        }
        EnsureGaplessSequence(steps);

        SourceRoutingId = sourceRoutingId;
        RoutingCodeSnapshot = routingCode;
        RoutingNameSnapshot = routingName;
        RoutingSnapshotVersion = routingVersion;

        _steps.Clear();
        var ordered = steps.OrderBy(s => s.StepNumber).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var input = i == 0 ? PlannedQuantity : 0m;
            _steps.Add(new ProductionJobStep(Id, ordered[i], input));
        }
        Touch();
    }

    public void Release(Guid warehouseId, DateTime utcNow)
    {
        EnsureTransitionAllowed(ProductionJobStatus.Released);
        if (_steps.Count == 0)
        {
            throw new ProductionJobHasNoStepsException();
        }
        Status = ProductionJobStatus.Released;
        WarehouseId = warehouseId;
        ReleasedAtUtc = AsUtc(utcNow);
        CurrentStepNumber = _steps.Min(s => s.StepNumber);
        Touch();
    }

    public void PutOnHold(DateTime utcNow)
    {
        EnsureTransitionAllowed(ProductionJobStatus.OnHold);
        Status = ProductionJobStatus.OnHold;
        Touch();
    }

    public void Resume(DateTime utcNow)
    {
        if (Status != ProductionJobStatus.OnHold)
        {
            throw new InvalidProductionJobTransitionException(Status.ToString(), "Resumed");
        }
        Status = StartedAtUtc.HasValue ? ProductionJobStatus.InProgress : ProductionJobStatus.Released;
        Touch();
    }

    public void Cancel(string? reason, DateTime utcNow)
    {
        if (IsTerminal)
        {
            throw new InvalidProductionJobTransitionException(Status.ToString(), ProductionJobStatus.Cancelled.ToString());
        }
        Status = ProductionJobStatus.Cancelled;
        CancelledAtUtc = AsUtc(utcNow);
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CurrentStepNumber = null;
        Touch();
    }

    public ProductionJobStep StartStep(int stepNumber, Guid operatorId, DateTime utcNow)
    {
        EnsureExecuting();
        var step = RequireStep(stepNumber);
        step.Start(operatorId, utcNow);
        if (Status == ProductionJobStatus.Released)
        {
            Status = ProductionJobStatus.InProgress;
            StartedAtUtc = AsUtc(utcNow);
        }
        
        _logs.Add(new ProductionJobLog(Id, step.Id, operatorId, "Start", utcNow, null, null, null));
        
        Touch();
        return step;
    }

    public void FinishStep(
        int stepNumber,
        decimal goodQuantity,
        decimal scrappedQuantity,
        Guid? scrapReasonCodeId,
        decimal? actualSetupMinutes,
        decimal? actualRunMinutes,
        Guid operatorId,
        DateTime utcNow)
    {
        if (Status != ProductionJobStatus.InProgress)
        {
            throw new InvalidProductionJobTransitionException(Status.ToString(), "FinishStep");
        }
        var step = RequireStep(stepNumber);
        step.Finish(goodQuantity, scrappedQuantity, scrapReasonCodeId, actualSetupMinutes, actualRunMinutes, operatorId, utcNow);
        ScrappedQuantity += scrappedQuantity;

        _logs.Add(new ProductionJobLog(Id, step.Id, operatorId, "Finish", utcNow, scrapReasonCodeId?.ToString(), goodQuantity, actualRunMinutes.HasValue ? (int)actualRunMinutes.Value : null));


        var next = _steps
            .Where(s => s.StepNumber > stepNumber
                && s.Status is ProductionJobStepStatus.Pending or ProductionJobStepStatus.Reopened)
            .OrderBy(s => s.StepNumber)
            .FirstOrDefault();
        next?.SetInputQuantity(goodQuantity);

        RecomputeCursor();
        Touch();
    }

    public void SkipStep(int stepNumber, DateTime utcNow)
    {
        EnsureExecuting();
        var step = RequireStep(stepNumber);
        if (!step.IsOptional)
        {
            throw new NonOptionalStepCannotBeSkippedException(stepNumber);
        }
        step.Skip(utcNow);
        RecomputeCursor();
        Touch();
    }

    public void ReworkToStep(int targetStepNumber, int fromStepNumber, string reason, DateTime utcNow)
    {
        if (Status != ProductionJobStatus.InProgress)
        {
            throw new InvalidProductionJobTransitionException(Status.ToString(), "Rework");
        }
        if (targetStepNumber >= fromStepNumber)
        {
            throw new ReworkTargetInvalidException(targetStepNumber, fromStepNumber);
        }
        var target = _steps.FirstOrDefault(s => s.StepNumber == targetStepNumber);
        if (target is null || target.Status != ProductionJobStepStatus.Completed)
        {
            throw new ReworkTargetInvalidException(targetStepNumber, fromStepNumber);
        }

        target.ReopenForRework(fromStepNumber, reason, utcNow);
        foreach (var s in _steps.Where(x =>
            x.StepNumber > targetStepNumber
            && x.StepNumber <= fromStepNumber
            && x.Status == ProductionJobStepStatus.Completed))
        {
            s.ReopenToPending();
        }
        CurrentStepNumber = targetStepNumber;
        Touch();
    }

    public void MarkCompleted(decimal completedQuantity, Guid warehouseId, DateTime utcNow)
    {
        EnsureTransitionAllowed(ProductionJobStatus.Completed);
        if (completedQuantity < 0m || completedQuantity > PlannedQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(completedQuantity), "Completed quantity must be between 0 and the planned quantity.");
        }
        Status = ProductionJobStatus.Completed;
        CompletedQuantity = completedQuantity;
        WarehouseId = warehouseId;
        CompletedAtUtc = AsUtc(utcNow);
        CurrentStepNumber = null;
        Touch();
    }

    private void RecomputeCursor()
    {
        var active = _steps
            .Where(s => s.Status is not (ProductionJobStepStatus.Completed or ProductionJobStepStatus.Skipped))
            .OrderBy(s => s.StepNumber)
            .FirstOrDefault();
        if (active is not null)
        {
            CurrentStepNumber = active.StepNumber;
        }
        else if (AllRequiredStepsDone)
        {
            Status = ProductionJobStatus.ReadyToComplete;
            CurrentStepNumber = null;
        }
    }

    private ProductionJobStep RequireStep(int stepNumber) =>
        _steps.FirstOrDefault(s => s.StepNumber == stepNumber)
        ?? throw new InvalidProductionJobStepTransitionException("Missing", stepNumber.ToString());

    private void EnsureExecuting()
    {
        if (Status is not (ProductionJobStatus.Released or ProductionJobStatus.InProgress))
        {
            throw new InvalidProductionJobTransitionException(Status.ToString(), "StepExecution");
        }
    }

    private static void EnsureGaplessSequence(IReadOnlyList<ProductionJobStepSnapshot> steps)
    {
        var numbers = steps.Select(s => s.StepNumber).OrderBy(n => n).ToList();
        for (var i = 0; i < numbers.Count; i++)
        {
            if (numbers[i] != i + 1)
            {
                throw new ProductionJobHasNoStepsException();
            }
        }
    }

    private bool IsTransitionAllowed(ProductionJobStatus target) => Status switch
    {
        ProductionJobStatus.Draft => target is ProductionJobStatus.Released or ProductionJobStatus.Cancelled,
        ProductionJobStatus.Released => target is ProductionJobStatus.InProgress or ProductionJobStatus.OnHold or ProductionJobStatus.Cancelled,
        ProductionJobStatus.InProgress => target is ProductionJobStatus.ReadyToComplete or ProductionJobStatus.OnHold or ProductionJobStatus.Cancelled,
        ProductionJobStatus.OnHold => target is ProductionJobStatus.InProgress or ProductionJobStatus.Released or ProductionJobStatus.Cancelled,
        ProductionJobStatus.ReadyToComplete => target is ProductionJobStatus.Completed or ProductionJobStatus.Cancelled,
        _ => false,
    };

    private void EnsureTransitionAllowed(ProductionJobStatus target)
    {
        if (!IsTransitionAllowed(target))
        {
            throw new InvalidProductionJobTransitionException(Status.ToString(), target.ToString());
        }
    }

    private static DateTime? AsUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
