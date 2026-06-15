using CoreAlign.Application.Sales.OrderTemplates.Handlers;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Sales.OrderTemplates.Jobs;

public sealed class RecurringOrderJob
{
    private const int MaxBatch = 100;

    private readonly IOrderTemplateRepository _repository;
    private readonly ITenantContext _tenant;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecurringOrderJob> _logger;

    public RecurringOrderJob(
        IOrderTemplateRepository repository,
        ITenantContext tenant,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<RecurringOrderJob> logger)
    {
        _repository = repository;
        _tenant = tenant;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var due = await _repository.GetDueAsync(nowUtc, MaxBatch, cancellationToken);
        if (due.Count == 0)
        {
            _logger.LogDebug("RecurringOrderJob found no due templates at {NowUtc:o}.", nowUtc);
            return;
        }

        var byTenant = due.GroupBy(t => t.TenantId);
        var created = 0;
        var failed = 0;

        foreach (var group in byTenant)
        {
            using var scope = _tenant.PushScope(group.Key);
            foreach (var template in group)
            {
                if (!template.IsDue(nowUtc))
                {
                    continue;
                }
                try
                {
                    var orderId = await RecurringOrderRunner.RunOnceAsync(template, _mediator, nowUtc, cancellationToken);
                    _repository.Update(template);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    created++;
                    _logger.LogInformation(
                        "RecurringOrderJob created order {OrderId} from template {TemplateId} for tenant {TenantId}.",
                        orderId, template.Id, group.Key);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "RecurringOrderJob failed to run template {TemplateId} for tenant {TenantId}.",
                        template.Id, group.Key);
                }
            }
        }

        _logger.LogInformation(
            "RecurringOrderJob processed {Total} due templates ({Created} created, {Failed} failed) at {NowUtc:o}.",
            due.Count, created, failed, nowUtc);
    }
}
