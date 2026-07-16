using System.Text.RegularExpressions;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Manufacturing;

public partial class RoutingStep : TenantEntity
{
    public Guid RoutingId { get; private set; }
    public int StepNumber { get; private set; }
    public Guid WorkCenterId { get; private set; }
    public string OperationName { get; private set; } = string.Empty;
    public RoutingOperationType OperationType { get; private set; }
    public decimal SetupTimeMinutes { get; private set; }
    public decimal RunTimeMinutesPerUnit { get; private set; }
    public decimal? RunTimeMinutesPerSqm { get; private set; }
    public decimal ScrapPercentage { get; private set; }
    public string? Instructions { get; private set; }
    public bool IsOptional { get; private set; }

    protected RoutingStep() { }

    public RoutingStep(
        Guid routingId,
        int stepNumber,
        Guid workCenterId,
        string operationName,
        RoutingOperationType operationType,
        decimal setupTimeMinutes,
        decimal runTimeMinutesPerUnit,
        decimal? runTimeMinutesPerSqm = null,
        decimal scrapPercentage = 0m,
        string? instructions = null,
        bool isOptional = false)
    {
        if (stepNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(stepNumber), "Step number must be 1 or greater.");
        if (workCenterId == Guid.Empty)
            throw new ArgumentException("Work center is required.", nameof(workCenterId));
        var normalizedName = NormalizeName(operationName);
        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new ArgumentException("Operation name is required.", nameof(operationName));
        if (setupTimeMinutes < 0m)
            throw new ArgumentOutOfRangeException(nameof(setupTimeMinutes), "Setup time cannot be negative.");
        if (runTimeMinutesPerUnit < 0m)
            throw new ArgumentOutOfRangeException(nameof(runTimeMinutesPerUnit), "Run time cannot be negative.");
        if (runTimeMinutesPerSqm is < 0m)
            throw new ArgumentOutOfRangeException(nameof(runTimeMinutesPerSqm), "Run time per m² cannot be negative.");
        if (scrapPercentage is < 0m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(scrapPercentage), "Scrap percentage must be between 0 and 100.");

        RoutingId = routingId;
        StepNumber = stepNumber;
        WorkCenterId = workCenterId;
        OperationName = normalizedName;
        OperationType = operationType;
        SetupTimeMinutes = setupTimeMinutes;
        RunTimeMinutesPerUnit = runTimeMinutesPerUnit;
        RunTimeMinutesPerSqm = runTimeMinutesPerSqm;
        ScrapPercentage = scrapPercentage;
        Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();
        IsOptional = isOptional;
    }

    public void AttachToRouting(Guid routingId)
    {
        RoutingId = routingId;
    }

    private static string NormalizeName(string value) =>
        WhitespaceRegex().Replace((value ?? string.Empty).Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
