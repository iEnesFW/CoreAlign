using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class IssueCreditNoteCommandHandler : IRequestHandler<IssueCreditNoteCommand, InvoiceDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IAccountingPeriodRepository _periodRepository;
    private readonly ITenantContext _tenantContext;

    public IssueCreditNoteCommandHandler(
        IInvoiceRepository invoiceRepository,
        IDocumentSequenceRepository sequenceRepository,
        IAccountingPeriodRepository periodRepository,
        ITenantContext tenantContext)
    {
        _invoiceRepository = invoiceRepository;
        _sequenceRepository = sequenceRepository;
        _periodRepository = periodRepository;
        _tenantContext = tenantContext;
    }

    public async Task<InvoiceDto> Handle(IssueCreditNoteCommand request, CancellationToken cancellationToken)
    {
        var origin = await _invoiceRepository.GetWithLinesAsync(request.InvoiceId, cancellationToken)
            ?? throw new InvoiceNotFoundException();
        _tenantContext.EnsureSameTenant(origin.TenantId);

        if (!origin.IsIssued)
        {
            throw new InvoiceStatusTransitionException(origin.Status.ToString(), "issue credit note");
        }

        // WHY a credit note cannot be credited: Invoice.IssueCreditNote copies the origin Type, so
        // the second note is another CreditNote and its issue event credits AR a second time
        // instead of restoring it. The over-credit guard cannot see it either — it matches on
        // OriginInvoiceId, and the second note points at the first, not at the real invoice.
        if (origin.Type == InvoiceType.CreditNote)
        {
            throw new CreditNoteCannotBeCreditedException(origin.InvoiceNumber);
        }

        var durableReplay = await TryReplayFromReturnRequestAsync(origin, request, cancellationToken);
        if (durableReplay is not null)
        {
            return durableReplay;
        }

        var byId = origin.Lines.ToDictionary(l => l.Id);
        var requestedTotals = new Dictionary<Guid, decimal>();
        foreach (var input in request.Lines)
        {
            if (!byId.TryGetValue(input.InvoiceLineId, out _))
            {
                throw new CannotIssueCreditNoteException(
                    $"Invoice line {input.InvoiceLineId} not found on invoice {origin.InvoiceNumber}.");
            }
            requestedTotals[input.InvoiceLineId] =
                requestedTotals.GetValueOrDefault(input.InvoiceLineId) + input.Quantity;
        }

        var alreadyCreditedByLine = await GetAlreadyCreditedByLineAsync(origin.Id, cancellationToken);

        var creditLines = new List<InvoiceLine>();
        foreach (var (invoiceLineId, qty) in requestedTotals)
        {
            var source = byId[invoiceLineId];
            var alreadyCredited = alreadyCreditedByLine.GetValueOrDefault(invoiceLineId);
            var remaining = Math.Max(0m, source.Quantity - alreadyCredited);
            if (qty > remaining)
            {
                throw new CannotIssueCreditNoteException(
                    $"Cannot credit {qty} of '{source.ProductSku}' — only {remaining} remains creditable.");
            }
            creditLines.Add(BuildCreditLine(source, qty));
        }

        var now = DateTime.UtcNow;
        var period = await _periodRepository.GetByDateAsync(now.Date, cancellationToken);
        period?.EnsurePostingAllowed(now);

        var creditNumber = await _sequenceRepository.ConsumeAsync(
            DocumentSequenceType.CreditNoteNumber, now, cancellationToken);

        var creditNote = Invoice.IssueCreditNote(
            origin,
            creditNumber,
            now,
            creditLines,
            request.Reason,
            approvedByUserId: null,
            returnRequestId: request.ReturnRequestId);

        await _invoiceRepository.AddAsync(creditNote, cancellationToken);
        _invoiceRepository.Update(origin);
        if (origin.Customer is not null)
        {
            creditNote.Customer = origin.Customer;
        }

        return InvoiceMapper.ToDto(creditNote);
    }

    private async Task<InvoiceDto?> TryReplayFromReturnRequestAsync(
        Invoice origin,
        IssueCreditNoteCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ReturnRequestId is null)
        {
            return null;
        }

        var existing = await _invoiceRepository.GetCreditNotesForInvoiceAsync(origin.Id, cancellationToken);
        var match = existing.FirstOrDefault(note =>
            note.ReturnRequestId == request.ReturnRequestId
            && note.Status != InvoiceStatus.Cancelled
            && note.Status != InvoiceStatus.Void);
        if (match is null)
        {
            return null;
        }

        if (origin.Customer is not null)
        {
            match.Customer = origin.Customer;
        }
        return InvoiceMapper.ToDto(match);
    }

    private async Task<Dictionary<Guid, decimal>> GetAlreadyCreditedByLineAsync(
        Guid originInvoiceId,
        CancellationToken cancellationToken)
    {
        var existing = await _invoiceRepository.GetCreditNotesForInvoiceAsync(originInvoiceId, cancellationToken);
        return CreditNoteCalculations.SumCreditedByOriginLine(existing);
    }

    private static InvoiceLine BuildCreditLine(InvoiceLine source, decimal quantity)
    {
        // Scale the source line's absolute discount to the credited quantity so a
        // partial credit reverses only its share; a full credit reverses all of it.
        // Percentage discounts carry verbatim (they re-derive from the new quantity).
        var qtyFraction = source.Quantity > 0m ? quantity / source.Quantity : 0m;
        var scaledLineDiscount = Math.Round(source.LineDiscountAmount * qtyFraction, 4);
        var line = new InvoiceLine(
            source.ProductId ?? Guid.Empty,
            source.ProductSku,
            source.ProductName,
            quantity,
            source.UnitPrice);
        line.ApplyPricing(
            quantity: quantity,
            unitPrice: source.UnitPrice,
            lineDiscountPercent: source.LineDiscountPercent,
            lineDiscountAmount: scaledLineDiscount,
            taxRatePercent: source.TaxRatePercent,
            taxRateId: source.TaxRateId,
            isTaxInclusive: source.IsTaxInclusive,
            withholdingRatePercent: source.WithholdingRatePercent,
            uomId: source.UomId,
            uomCode: source.UomCode,
            description: source.Description,
            revenueAccountCode: source.RevenueAccountCode,
            costCenter: null,
            project: null,
            originOrderLineId: source.Id,
            // WHY the GİB code travels with the credit: withholding is a fraction of the line's VAT
            // (7/10 for code 617), not a percentage of the net, so a credit line without the code
            // computes zero withholding — it would over-credit AR and strand the 193 receivable.
            withholdingTaxCodeId: source.WithholdingTaxCodeId,
            withholdingCode: source.WithholdingCode,
            withholdingNumerator: source.WithholdingNumerator,
            withholdingDenominator: source.WithholdingDenominator);
        return line;
    }
}
