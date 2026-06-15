using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CoreAlign.Application.Common.Caching;
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
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromMinutes(10);

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IAccountingPeriodRepository _periodRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDistributedCacheService? _cache;

    public IssueCreditNoteCommandHandler(
        IInvoiceRepository invoiceRepository,
        IDocumentSequenceRepository sequenceRepository,
        IAccountingPeriodRepository periodRepository,
        ITenantContext tenantContext,
        IDistributedCacheService? cache = null)
    {
        _invoiceRepository = invoiceRepository;
        _sequenceRepository = sequenceRepository;
        _periodRepository = periodRepository;
        _tenantContext = tenantContext;
        _cache = cache;
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

        var durableReplay = await TryReplayFromReturnRequestAsync(origin, request, cancellationToken);
        if (durableReplay is not null)
        {
            return durableReplay;
        }

        var fingerprint = BuildFingerprint(request);
        var cacheKey = _cache is not null ? _cache.BuildKey(nameof(CacheRegion.Generic), origin.TenantId, fingerprint) : null;
        if (_cache is not null && cacheKey is not null)
        {
            var cached = await _cache.GetAsync<InvoiceDto>(nameof(CacheRegion.Generic), cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
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

        var dto = InvoiceMapper.ToDto(creditNote);
        if (_cache is not null && cacheKey is not null)
        {
            await _cache.SetAsync(nameof(CacheRegion.Generic), cacheKey, dto, IdempotencyWindow, cancellationToken);
        }
        return dto;
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

    private static string BuildFingerprint(IssueCreditNoteCommand request)
    {
        if (request.OperationId is { } operationId && operationId != Guid.Empty)
        {
            return $"credit-note:op:{operationId:N}";
        }

        var lines = request.Lines
            .GroupBy(l => l.InvoiceLineId)
            .Select(g => (LineId: g.Key, Quantity: g.Sum(l => l.Quantity)))
            .OrderBy(t => t.LineId)
            .Select(t => $"{t.LineId:N}={t.Quantity.ToString(CultureInfo.InvariantCulture)}");

        var raw = $"{request.InvoiceId:N}|{string.Join(",", lines)}|{request.ReturnRequestId:N}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"credit-note:hash:{Convert.ToHexString(hash)}";
    }

    private async Task<Dictionary<Guid, decimal>> GetAlreadyCreditedByLineAsync(
        Guid originInvoiceId,
        CancellationToken cancellationToken)
    {
        var existing = await _invoiceRepository.GetCreditNotesForInvoiceAsync(originInvoiceId, cancellationToken);
        var totals = new Dictionary<Guid, decimal>();
        foreach (var note in existing)
        {
            if (note.Status == InvoiceStatus.Cancelled || note.Status == InvoiceStatus.Void)
            {
                continue;
            }
            foreach (var line in note.Lines)
            {
                if (line.OriginOrderLineId is null) continue;
                var key = line.OriginOrderLineId.Value;
                totals[key] = totals.GetValueOrDefault(key) + line.Quantity;
            }
        }
        return totals;
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
            originOrderLineId: source.Id);
        return line;
    }
}
