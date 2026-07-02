using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Recurring.Commands;
using CoreAlign.Domain.Entities.Invoices;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Recurring.Handlers;

public class RunRecurringInvoiceNowCommandHandler : IRequestHandler<RunRecurringInvoiceNowCommand, Guid?>
{
    private readonly IRecurringInvoiceTemplateRepository _repository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public RunRecurringInvoiceNowCommandHandler(
        IRecurringInvoiceTemplateRepository repository,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid?> Handle(RunRecurringInvoiceNowCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new RecurringInvoiceTemplateNotFoundException();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.FromJob)
        {
            if (!template.IsDue(today))
            {
                return null;
            }
        }
        else if (template.Status != Domain.Enums.RecurringInvoiceStatus.Active)
        {
            throw new InvalidRecurringInvoiceTransitionException(
                "Only an active template can be generated. Resume it first.");
        }

        var invoiceId = await RecurringInvoiceRunner.RunOnceAsync(template, _mediator, cancellationToken);
        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return invoiceId;
    }
}

internal static class RecurringInvoiceRunner
{
    public static async Task<Guid> RunOnceAsync(
        RecurringInvoiceTemplate template,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (template.Lines.Count == 0)
        {
            throw new InvalidInvoiceLineException("Recurring invoice template has no lines.");
        }

        var periodKey = template.NextRunDate;
        var issueDate = DateTime.SpecifyKind(periodKey.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var lines = template.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new StandaloneInvoiceLineInput(
                ProductId: l.ProductId,
                ProductSku: l.ProductSku,
                ProductName: l.ProductName,
                Description: l.Description,
                Quantity: l.Quantity,
                UnitPrice: l.UnitPrice,
                TaxRatePercent: l.TaxRatePercent,
                LineDiscountPercent: l.LineDiscountPercent,
                LineDiscountAmount: l.LineDiscountAmount,
                TaxRateId: l.TaxRateId,
                IsTaxInclusive: l.IsTaxInclusive,
                WithholdingRatePercent: l.WithholdingRatePercent,
                UomId: l.UomId,
                UomCode: l.UomCode))
            .ToList();

        var command = new CreateStandaloneInvoiceCommand(
            CustomerId: template.CustomerId,
            IssueDate: issueDate,
            Currency: template.Currency,
            Lines: lines,
            DueDays: template.DueDays,
            PaymentTermsId: template.PaymentTermsId,
            HeaderDiscountPercent: template.HeaderDiscountPercent,
            HeaderDiscountAmount: template.HeaderDiscountAmount,
            ShippingCost: template.ShippingCost,
            RoundingAdjustment: template.RoundingAdjustment,
            InternalNotes: template.InternalNotes,
            PublicNotes: template.PublicNotes);

        var dto = await mediator.Send(command, cancellationToken);
        template.RecordOccurrence(periodKey, dto.Id, DateTime.UtcNow);
        return dto.Id;
    }
}
