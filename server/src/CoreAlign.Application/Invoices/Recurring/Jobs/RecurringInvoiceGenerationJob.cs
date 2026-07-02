using CoreAlign.Application.Invoices.Recurring.Commands;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Invoices.Recurring.Jobs;

public sealed class RecurringInvoiceGenerationJob
{
    private const int MaxBatch = 200;

    private readonly IRecurringInvoiceDataSource _data;
    private readonly ITenantContext _tenant;
    private readonly IMediator _mediator;
    private readonly ILogger<RecurringInvoiceGenerationJob> _logger;

    public RecurringInvoiceGenerationJob(
        IRecurringInvoiceDataSource data,
        ITenantContext tenant,
        IMediator mediator,
        ILogger<RecurringInvoiceGenerationJob> logger)
    {
        _data = data;
        _tenant = tenant;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var due = await _data.GetDueTemplatesAsync(today, MaxBatch, cancellationToken).ConfigureAwait(false);
        if (due.Count == 0)
        {
            _logger.LogDebug("RecurringInvoiceGenerationJob found no due templates on {Today}.", today);
            return;
        }

        var generated = 0;
        var failed = 0;

        foreach (var group in due.GroupBy(t => t.TenantId))
        {
            using var scope = _tenant.PushScope(group.Key);
            foreach (var snapshot in group)
            {
                try
                {
                    var invoiceId = await _mediator
                        .Send(new RunRecurringInvoiceNowCommand(snapshot.TemplateId, FromJob: true), cancellationToken)
                        .ConfigureAwait(false);
                    if (invoiceId.HasValue)
                    {
                        generated++;
                        _logger.LogInformation(
                            "RecurringInvoiceGenerationJob generated invoice {InvoiceId} from template {TemplateId} for tenant {TenantId}.",
                            invoiceId, snapshot.TemplateId, group.Key);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "RecurringInvoiceGenerationJob failed for template {TemplateId} tenant {TenantId}.",
                        snapshot.TemplateId, group.Key);
                }
            }
        }

        _logger.LogInformation(
            "RecurringInvoiceGenerationJob processed {Total} due template(s) ({Generated} generated, {Failed} failed) on {Today}.",
            due.Count, generated, failed, today);
    }
}
