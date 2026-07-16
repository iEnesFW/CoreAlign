using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.Manufacturing;

public record RoutingStepDraft(
    int StepNumber,
    Guid WorkCenterId,
    string OperationName,
    RoutingOperationType OperationType,
    decimal SetupTimeMinutes,
    decimal RunTimeMinutesPerUnit,
    decimal? RunTimeMinutesPerSqm,
    decimal ScrapPercentage,
    string? Instructions,
    bool IsOptional);

public class ProductionRouting : TenantEntity, IHasConcurrencyToken
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public RoutingStatus Status { get; private set; } = RoutingStatus.Draft;

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    private readonly List<RoutingStep> _steps = new();
    public IReadOnlyCollection<RoutingStep> Steps => _steps.AsReadOnly();

    protected ProductionRouting() { }

    public ProductionRouting(string code, string name, string? description = null)
    {
        AssignHeader(code, name, description);
        Status = RoutingStatus.Draft;
    }

    public void UpdateHeader(string code, string name, string? description)
    {
        if (Status == RoutingStatus.Archived)
        {
            throw new RoutingNotEditableException();
        }
        AssignHeader(code, name, description);
        Touch();
    }

    public void ReplaceSteps(IReadOnlyCollection<RoutingStepDraft> steps)
    {
        if (Status != RoutingStatus.Draft)
        {
            throw new RoutingNotEditableException();
        }
        if (steps is null || steps.Count == 0)
        {
            throw new RoutingHasNoStepsException();
        }
        EnsureGaplessSequence(steps);

        _steps.Clear();
        foreach (var draft in steps.OrderBy(s => s.StepNumber))
        {
            _steps.Add(new RoutingStep(
                Id,
                draft.StepNumber,
                draft.WorkCenterId,
                draft.OperationName,
                draft.OperationType,
                draft.SetupTimeMinutes,
                draft.RunTimeMinutesPerUnit,
                draft.RunTimeMinutesPerSqm,
                draft.ScrapPercentage,
                draft.Instructions,
                draft.IsOptional));
        }
        Touch();
    }

    public void Activate()
    {
        EnsureTransitionAllowed(RoutingStatus.Active);
        if (_steps.Count == 0)
        {
            throw new RoutingHasNoStepsException();
        }
        Status = RoutingStatus.Active;
        Touch();
    }

    public void Archive()
    {
        EnsureTransitionAllowed(RoutingStatus.Archived);
        Status = RoutingStatus.Archived;
        Touch();
    }

    public void RestoreToDraft()
    {
        EnsureTransitionAllowed(RoutingStatus.Draft);
        Status = RoutingStatus.Draft;
        Touch();
    }

    private void AssignHeader(string code, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Routing code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Routing name is required.", nameof(name));
        Code = code.Trim();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static void EnsureGaplessSequence(IReadOnlyCollection<RoutingStepDraft> steps)
    {
        var numbers = steps.Select(s => s.StepNumber).ToList();
        if (numbers.Distinct().Count() != numbers.Count)
        {
            var duplicate = numbers.GroupBy(n => n).First(g => g.Count() > 1).Key;
            throw new DuplicateRoutingStepException(duplicate);
        }
        var ordered = numbers.OrderBy(n => n).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i] != i + 1)
            {
                throw new RoutingStepsNotSequentialException();
            }
        }
    }

    private bool IsTransitionAllowed(RoutingStatus target) => Status switch
    {
        RoutingStatus.Draft => target is RoutingStatus.Active or RoutingStatus.Archived,
        RoutingStatus.Active => target is RoutingStatus.Archived,
        RoutingStatus.Archived => target is RoutingStatus.Draft,
        _ => false,
    };

    private void EnsureTransitionAllowed(RoutingStatus target)
    {
        if (!IsTransitionAllowed(target))
        {
            throw new InvalidRoutingTransitionException(Status.ToString(), target.ToString());
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
