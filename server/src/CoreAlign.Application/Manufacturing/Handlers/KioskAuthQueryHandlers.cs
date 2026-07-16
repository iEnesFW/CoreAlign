using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Handlers;

public class VerifyOperatorPinQueryHandler : IRequestHandler<Queries.VerifyOperatorPinQuery, Queries.KioskAuthResult?>
{
    private readonly IWorkCenterOperatorRepository _operators;
    private readonly ITenantContext _tenant;

    public VerifyOperatorPinQueryHandler(IWorkCenterOperatorRepository operators, ITenantContext tenant)
    {
        _operators = operators;
        _tenant = tenant;
    }

    public async Task<Queries.KioskAuthResult?> Handle(Queries.VerifyOperatorPinQuery request, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var op = await _operators.GetByIdAsync(tenantId, request.OperatorId, ct)
            ?? throw new WorkCenterOperatorNotFoundException(request.OperatorId);

        if (!op.IsActive)
        {
            return null;
        }

        // Simplistic check for Phase 4 Kiosk Mode
        // In real-world scenario, this should use hashed PinCode check.
        return op.PinCode == request.PinCode ? new Queries.KioskAuthResult(op.WorkCenterId, op.EmployeeId) : null;
    }
}

public class GetActiveKioskStepsQueryHandler : IRequestHandler<Queries.GetActiveKioskStepsQuery, IReadOnlyList<Queries.KioskStepDto>>
{
    private readonly IProductionJobRepository _jobs;
    private readonly IProductRepository _products;
    private readonly ITenantContext _tenant;

    public GetActiveKioskStepsQueryHandler(
        IProductionJobRepository jobs,
        IProductRepository products,
        ITenantContext tenant)
    {
        _jobs = jobs;
        _products = products;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<Queries.KioskStepDto>> Handle(Queries.GetActiveKioskStepsQuery request, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        
        var jobs = await _jobs.GetByStatusAsync(
            tenantId, 
            new[] { Domain.Enums.ProductionJobStatus.Released, Domain.Enums.ProductionJobStatus.InProgress }, 
            ct);

        var activeSteps = new List<Queries.KioskStepDto>();

        var productIds = jobs.Select(j => j.ProductId).Distinct().ToArray();
        var products = await _products.GetByIdsAsync(productIds, ct);

        foreach (var job in jobs)
        {
            var productName = products.TryGetValue(job.ProductId, out var product) ? product.Name : "Unknown Product";

            foreach (var step in job.Steps.Where(s => s.WorkCenterId == request.WorkCenterId && s.Status is Domain.Enums.ProductionJobStepStatus.Pending or Domain.Enums.ProductionJobStepStatus.InProgress))
            {
                activeSteps.Add(new Queries.KioskStepDto(
                    job.Id,
                    job.JobNumber,
                    productName,
                    step.StepNumber,
                    step.OperationName,
                    step.InputQuantity,
                    step.Status.ToString(),
                    step.StartedAtUtc,
                    step.AssignedOperatorId
                ));
            }
        }

        return activeSteps.OrderBy(s => s.JobNumber).ThenBy(s => s.StepNumber).ToList();
    }
}
