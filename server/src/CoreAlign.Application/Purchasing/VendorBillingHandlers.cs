using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Purchasing;

internal static class VendorGLLines
{
    public static IReadOnlyList<GLPostingLine> Bill(decimal subtotal, decimal tax, decimal total, bool inventoryPurchase, bool reverse)
    {
        var debitKey = inventoryPurchase ? GLPostingKey.GoodsReceiptClearing : GLPostingKey.PurchaseExpense;
        return reverse
            ? new[]
            {
                new GLPostingLine(GLPostingKey.AccountsPayable, total, 0m),
                new GLPostingLine(debitKey, 0m, subtotal),
                new GLPostingLine(GLPostingKey.InputVat, 0m, tax),
            }
            : new[]
            {
                new GLPostingLine(debitKey, subtotal, 0m),
                new GLPostingLine(GLPostingKey.InputVat, tax, 0m),
                new GLPostingLine(GLPostingKey.AccountsPayable, 0m, total),
            };
    }

    // PO-linked inventory bill with lines: split the debit into a GR/IR clearing
    // leg at RECEIPT cost (qty * PoUnitCost) and a PurchasePriceVariance leg for the
    // in-tolerance price difference, so 322 clears to exactly what the receipt
    // credited regardless of the billed price. VAT + AP stay as today.
    //
    // Variance is DERIVED as Subtotal - clearing (not summed per line) so that
    // clearing + variance == Subtotal exactly; with VAT == TaxAmount this makes
    // total debits (clearing + variance + tax) == Total == credit by construction
    // at any exchange rate, never throwing JournalEntryNotBalancedException.
    public static IReadOnlyList<GLPostingLine> BillWithLines(VendorBill bill, bool reverse = false)
    {
        var clearing = Math.Round(bill.Lines.Sum(l => l.ReceiptClearingCost), 4);
        var variance = Math.Round(bill.Subtotal - clearing, 4);
        return BuildLineAwareLegs(clearing, variance, bill.TaxAmount, bill.Total, reverse);
    }

    // Shared leg builder for both posting (reverse:false) and cancel (reverse:true)
    // so the void mirrors exactly what was posted. On reverse the debit/credit of
    // every leg flips while the variance keeps its economic sign via the swap.
    public static IReadOnlyList<GLPostingLine> BuildLineAwareLegs(
        decimal clearing, decimal variance, decimal tax, decimal total, bool reverse)
    {
        var lines = new List<GLPostingLine>
        {
            reverse
                ? new GLPostingLine(GLPostingKey.GoodsReceiptClearing, 0m, clearing)
                : new GLPostingLine(GLPostingKey.GoodsReceiptClearing, clearing, 0m),
        };
        if (variance > 0m)
        {
            lines.Add(reverse
                ? new GLPostingLine(GLPostingKey.PurchasePriceVariance, 0m, variance)
                : new GLPostingLine(GLPostingKey.PurchasePriceVariance, variance, 0m));
        }
        else if (variance < 0m)
        {
            lines.Add(reverse
                ? new GLPostingLine(GLPostingKey.PurchasePriceVariance, -variance, 0m)
                : new GLPostingLine(GLPostingKey.PurchasePriceVariance, 0m, -variance));
        }
        lines.Add(reverse
            ? new GLPostingLine(GLPostingKey.InputVat, 0m, tax)
            : new GLPostingLine(GLPostingKey.InputVat, tax, 0m));
        lines.Add(reverse
            ? new GLPostingLine(GLPostingKey.AccountsPayable, total, 0m)
            : new GLPostingLine(GLPostingKey.AccountsPayable, 0m, total));
        return lines;
    }

    // A bill posts through the line-aware split only when it carries PO-linked
    // lines; header-only and PO-less bills keep the verbatim single-debit path.
    public static bool HasPoLinkedLines(VendorBill bill) =>
        bill.Lines.Count > 0 && bill.Lines.Any(l => l.PurchaseOrderLineId is not null);

    public static IReadOnlyList<GLPostingLine> BuildPostLines(VendorBill bill) =>
        HasPoLinkedLines(bill)
            ? BillWithLines(bill)
            : Bill(bill.Subtotal, bill.TaxAmount, bill.Total, bill.PurchaseOrderId is not null, reverse: false);

    // Reversal legs for a cancelled line-aware bill, prorated to the still-open
    // portion (factor = due / Total). The SAME line-aware split is reversed —
    // clearing at receipt cost, variance = Subtotal - clearing — each scaled by
    // the factor, so a FULL cancel (factor == 1) nets 322 and PPV to exactly
    // zero against the original post. AP is reversed at the open amount directly;
    // any sub-cent proration drift is absorbed by the GL residual nudge.
    public static IReadOnlyList<GLPostingLine> BillWithLinesReversal(VendorBill bill, decimal due)
    {
        var factor = bill.Total == 0m ? 0m : due / bill.Total;
        var fullClearing = Math.Round(bill.Lines.Sum(l => l.ReceiptClearingCost), 4);
        var fullVariance = Math.Round(bill.Subtotal - fullClearing, 4);
        var clearing = Math.Round(fullClearing * factor, 4);
        var variance = Math.Round(fullVariance * factor, 4);
        var tax = Math.Round(bill.TaxAmount * factor, 4);
        return BuildLineAwareLegs(clearing, variance, tax, due, reverse: true);
    }
}

// Builds VendorBillLine entities from the command input, resolving product
// identity and SNAPSHOTting the matched PurchaseOrderLine.UnitCost into
// PoUnitCost. PO-less lines force PoUnitCost = UnitPrice so PriceVariance is
// zero and they post through the unchanged single-debit path.
internal static class VendorBillLineFactory
{
    public static async Task<List<VendorBillLine>> BuildAsync(
        IReadOnlyList<VendorBillLineInput> inputs,
        Guid? purchaseOrderId,
        IProductRepository products,
        IPurchaseOrderRepository orders,
        CancellationToken ct)
    {
        var productMap = await products.GetByIdsAsync(inputs.Select(l => l.ProductId).Distinct(), ct);

        PurchaseOrder? po = null;
        if (purchaseOrderId is { } poId && inputs.Any(l => l.PurchaseOrderLineId is not null))
        {
            po = await orders.GetByIdAsync(poId, ct);
        }

        var lines = new List<VendorBillLine>(inputs.Count);
        foreach (var input in inputs)
        {
            productMap.TryGetValue(input.ProductId, out var product);
            var sku = product?.Sku ?? string.Empty;
            var name = product?.Name ?? string.Empty;

            decimal poUnitCost = input.UnitPrice;
            if (input.PurchaseOrderLineId is { } poLineId)
            {
                var poLine = po?.Lines.FirstOrDefault(l => l.Id == poLineId)
                    ?? throw new PurchaseOrderLineNotFoundForBillException();
                poUnitCost = poLine.UnitCost;
            }

            lines.Add(new VendorBillLine(
                input.ProductId, sku, name, input.Quantity, input.UnitPrice,
                poUnitCost: poUnitCost,
                purchaseOrderLineId: input.PurchaseOrderLineId,
                taxRatePercent: input.TaxRatePercent));
        }
        return lines;
    }
}

internal static class ThreeWayMatchEvaluator
{
    // Evaluates the two per-line gates against the matched PO line. A bill raised
    // against a PO but left header-only, or carrying lines not linked to a PO line,
    // cannot be verified against received quantities/prices — it is held for approval
    // rather than posting blind (bypassing the tolerance gate). Returns a hold reason
    // when any check breaches, otherwise null (post straight through).
    public static string? Breach(VendorBill bill, PurchaseOrder? po, ThreeWayMatchTolerance policy)
    {
        if (!policy.Enabled) return null;

        // The caller only invokes Breach when bill.PurchaseOrderId is set, so a null PO here means
        // the referenced order does not resolve in-tenant (deleted / cross-tenant / stale id) — it
        // cannot be three-way matched and must NOT post blind; hold it for approval.
        if (po is null)
        {
            return "Bill references a purchase order that could not be resolved and requires approval.";
        }

        if (bill.Lines.Count == 0)
        {
            return "Header-only bill against a purchase order requires approval (no lines to three-way match).";
        }

        foreach (var line in bill.Lines)
        {
            if (line.PurchaseOrderLineId is not { } poLineId)
            {
                return $"Line {line.LineNumber} is not linked to a purchase order line and requires approval.";
            }
            var poLine = po.Lines.FirstOrDefault(l => l.Id == poLineId);
            if (poLine is null)
            {
                return $"Line {line.LineNumber} references an unknown purchase order line and requires approval.";
            }

            var qtyCeiling = poLine.QuantityReceived * (1m + policy.QtyTolerancePercent / 100m) + policy.QtyToleranceAbsolute;
            if (poLine.QuantityBilled + line.Quantity > qtyCeiling)
            {
                return $"Quantity over-billed beyond tolerance on line {line.LineNumber}.";
            }

            var priceDelta = Math.Abs(line.UnitPrice - line.PoUnitCost);
            var priceCeilingPct = Math.Abs(line.PoUnitCost) * (policy.PriceTolerancePercent / 100m);
            if (priceDelta > priceCeilingPct && priceDelta > policy.PriceToleranceAbsolute)
            {
                return $"Unit price differs from PO beyond tolerance on line {line.LineNumber}.";
            }
        }
        return null;
    }
}

internal static class VendorBillingMapper
{
    public static VendorBillDto ToDto(VendorBill b) => new(
        b.Id, b.VendorId, b.VendorName, b.BillNumber, b.BillDate, b.DueDate, b.Currency,
        b.Subtotal, b.TaxAmount, b.Total, b.AmountPaid, b.AmountDue, b.Status, b.PurchaseOrderId, b.Notes, b.CreatedAtUtc);

    public static VendorPaymentDto ToDto(VendorPayment p) => new(
        p.Id, p.VendorId, p.VendorName, p.PaymentNumber, p.PaymentDate, p.Amount,
        p.AppliedAmount, p.UnappliedAmount, p.IsVoided, p.VoidedAtUtc, p.VoidReason,
        p.Currency, p.Method, p.VendorBillId, p.Notes, p.IsAdvance, p.CreatedAtUtc);

    public static VendorPaymentApplicationDto ToDto(VendorPaymentApplication a, string paymentNumber, string billNumber) => new(
        a.Id, a.VendorPaymentId, paymentNumber, a.VendorBillId, billNumber,
        a.AppliedAmount, a.AppliedAtUtc, a.AppliedByUserId, a.Notes);
}

internal static class VendorLedgerPoster
{
    public static async Task PostAsync(
        IVendorLedgerRepository ledger,
        IVendorRepository vendors,
        Guid vendorId,
        DateTime occurredAtUtc,
        LedgerEntryType entryType,
        decimal amount,
        string currency,
        decimal exchangeRate,
        LedgerSourceType sourceType,
        Guid? sourceDocumentId,
        string? sourceDocumentNumber,
        string? description,
        CancellationToken ct)
    {
        await ledger.AcquireAppendLockAsync(vendorId, ct);
        var last = await ledger.GetLastRunningBalanceAsync(vendorId, ct);
        var signed = entryType == LedgerEntryType.Credit ? Math.Abs(amount) : -Math.Abs(amount);
        var balance = Math.Round(last + signed, 4);

        var entry = new VendorLedgerEntry(vendorId, occurredAtUtc, occurredAtUtc.Date, entryType, amount,
            currency, exchangeRate, sourceType, sourceDocumentId, sourceDocumentNumber, description);
        entry.SetRunningBalance(balance);
        await ledger.AddAsync(entry, ct);

        var vendor = await vendors.GetByIdAsync(vendorId, ct);
        if (vendor is not null)
        {
            vendor.RecalculateBalance(balance, 0m, Math.Max(0m, balance));
            vendors.Update(vendor);
        }
    }
}

public class CreateVendorBillHandler : IRequestHandler<CreateVendorBillCommand, VendorBillDto>
{
    private readonly IVendorBillRepository _bills;
    private readonly IVendorRepository _vendors;
    private readonly IProductRepository _products;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IUnitOfWork _uow;

    public CreateVendorBillHandler(
        IVendorBillRepository bills,
        IVendorRepository vendors,
        IProductRepository products,
        IPurchaseOrderRepository orders,
        IUnitOfWork uow)
    {
        _bills = bills;
        _vendors = vendors;
        _products = products;
        _orders = orders;
        _uow = uow;
    }

    public async Task<VendorBillDto> Handle(CreateVendorBillCommand c, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(c.VendorId, ct) ?? throw new VendorNotFoundForPurchaseException();
        if (await _bills.BillNumberExistsAsync(c.VendorId, c.BillNumber.Trim(), null, ct))
        {
            throw new DuplicateVendorBillNumberException();
        }
        var bill = new VendorBill(vendor.Id, vendor.Name, c.BillNumber.Trim(), c.BillDate, c.Currency.ToUpperInvariant(),
            c.Subtotal, c.TaxAmount, c.DueDate, c.ExchangeRate, c.PurchaseOrderId, c.Notes);

        if (c.Lines is { Count: > 0 })
        {
            var lines = await VendorBillLineFactory.BuildAsync(c.Lines, c.PurchaseOrderId, _products, _orders, ct);
            bill.ReplaceLines(lines);
        }

        await _bills.AddAsync(bill, ct);
        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(bill);
    }
}

// Shared post-effects: the ledger credit + PPV-aware GL outbox enqueue + per-line
// RecordBill against the linked PO. Runs once — at post time for clean bills, at
// approval time for held bills — so GL + QuantityBilled commit atomically and
// never twice. Extracted to honour SOLID across PostVendorBill / ApproveVendorBill.
internal static class VendorBillPostEffects
{
    public static async Task ApplyAsync(
        VendorBill bill,
        IVendorLedgerRepository ledger,
        IVendorRepository vendors,
        IGLPostingOutbox outbox,
        IPurchaseOrderRepository orders,
        CancellationToken ct)
    {
        await VendorLedgerPoster.PostAsync(ledger, vendors, bill.VendorId, DateTime.UtcNow,
            LedgerEntryType.Credit, bill.Total, bill.Currency, bill.ExchangeRate,
            LedgerSourceType.Invoice, bill.Id, bill.BillNumber, $"Tedarikçi faturası {bill.BillNumber}", ct);

        await outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.VendorBill, bill.Id, bill.BillNumber, DateTime.UtcNow.Date,
            JournalEntryType.Mahsup, $"Tedarikçi faturası {bill.BillNumber}",
            VendorGLLines.BuildPostLines(bill),
            bill.Currency, bill.ExchangeRate), ct);

        if (bill.PurchaseOrderId is { } poId && VendorGLLines.HasPoLinkedLines(bill))
        {
            var po = await orders.GetByIdAsync(poId, ct);
            if (po is not null)
            {
                foreach (var line in bill.Lines)
                {
                    if (line.PurchaseOrderLineId is { } poLineId)
                    {
                        po.RecordLineBill(poLineId, line.Quantity);
                    }
                }
                orders.Update(po);
            }
        }
    }
}

public class PostVendorBillHandler : IRequestHandler<PostVendorBillCommand, VendorBillDto>
{
    private readonly IVendorBillRepository _bills;
    private readonly IVendorLedgerRepository _ledger;
    private readonly IVendorRepository _vendors;
    private readonly IGLPostingOutbox _outbox;
    private readonly IPurchaseOrderRepository _orders;
    private readonly ITolerancePolicyProvider _tolerance;
    private readonly IUnitOfWork _uow;

    public PostVendorBillHandler(
        IVendorBillRepository bills,
        IVendorLedgerRepository ledger,
        IVendorRepository vendors,
        IGLPostingOutbox outbox,
        IPurchaseOrderRepository orders,
        ITolerancePolicyProvider tolerance,
        IUnitOfWork uow)
    {
        _bills = bills;
        _ledger = ledger;
        _vendors = vendors;
        _outbox = outbox;
        _orders = orders;
        _tolerance = tolerance;
        _uow = uow;
    }

    public async Task<VendorBillDto> Handle(PostVendorBillCommand c, CancellationToken ct)
    {
        var bill = await _bills.GetByIdAsync(c.Id, ct) ?? throw new VendorBillNotFoundException();

        if (bill.PurchaseOrderId is { } poId)
        {
            var policy = await _tolerance.GetAsync(ct);
            if (policy.Enabled)
            {
                var po = await _orders.GetByIdAsync(poId, ct);
                var reason = ThreeWayMatchEvaluator.Breach(bill, po, policy);
                if (reason is not null)
                {
                    bill.PlaceOnHold(reason);
                    _bills.Update(bill);
                    await _uow.SaveChangesAsync(ct);
                    return VendorBillingMapper.ToDto(bill);
                }
            }
        }

        bill.Post();
        await VendorBillPostEffects.ApplyAsync(bill, _ledger, _vendors, _outbox, _orders, ct);
        _bills.Update(bill);
        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(bill);
    }
}

public class ApproveVendorBillHandler : IRequestHandler<ApproveVendorBillCommand, VendorBillDto>
{
    private readonly IVendorBillRepository _bills;
    private readonly IVendorLedgerRepository _ledger;
    private readonly IVendorRepository _vendors;
    private readonly IGLPostingOutbox _outbox;
    private readonly IPurchaseOrderRepository _orders;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;

    public ApproveVendorBillHandler(
        IVendorBillRepository bills,
        IVendorLedgerRepository ledger,
        IVendorRepository vendors,
        IGLPostingOutbox outbox,
        IPurchaseOrderRepository orders,
        ICurrentUserAccessor currentUser,
        IUnitOfWork uow)
    {
        _bills = bills;
        _ledger = ledger;
        _vendors = vendors;
        _outbox = outbox;
        _orders = orders;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<VendorBillDto> Handle(ApproveVendorBillCommand c, CancellationToken ct)
    {
        var bill = await _bills.GetByIdAsync(c.Id, ct) ?? throw new VendorBillNotFoundException();
        bill.ApproveAndPost(_currentUser.UserIdOrThrow());
        await VendorBillPostEffects.ApplyAsync(bill, _ledger, _vendors, _outbox, _orders, ct);
        _bills.Update(bill);
        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(bill);
    }
}

public class CancelVendorBillHandler : IRequestHandler<CancelVendorBillCommand, VendorBillDto>
{
    private readonly IVendorBillRepository _bills;
    private readonly IVendorLedgerRepository _ledger;
    private readonly IVendorRepository _vendors;
    private readonly IGLPostingOutbox _outbox;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IUnitOfWork _uow;

    public CancelVendorBillHandler(IVendorBillRepository bills, IVendorLedgerRepository ledger, IVendorRepository vendors, IGLPostingOutbox outbox, IPurchaseOrderRepository orders, IUnitOfWork uow)
    {
        _bills = bills;
        _ledger = ledger;
        _vendors = vendors;
        _outbox = outbox;
        _orders = orders;
        _uow = uow;
    }

    public async Task<VendorBillDto> Handle(CancelVendorBillCommand c, CancellationToken ct)
    {
        var bill = await _bills.GetByIdAsync(c.Id, ct) ?? throw new VendorBillNotFoundException();
        var wasPosted = bill.PostedAtUtc is not null;
        var due = bill.AmountDue;
        bill.Cancel();

        if (wasPosted && bill.PurchaseOrderId is { } poId && VendorGLLines.HasPoLinkedLines(bill))
        {
            var po = await _orders.GetByIdAsync(poId, ct);
            if (po is not null)
            {
                foreach (var line in bill.Lines)
                {
                    if (line.PurchaseOrderLineId is { } poLineId)
                    {
                        po.ReverseLineBill(poLineId, line.Quantity);
                    }
                }
                _orders.Update(po);
            }
        }

        if (wasPosted && due > 0m)
        {
            await VendorLedgerPoster.PostAsync(_ledger, _vendors, bill.VendorId, DateTime.UtcNow,
                LedgerEntryType.Debit, due, bill.Currency, bill.ExchangeRate,
                LedgerSourceType.InvoiceVoid, bill.Id, bill.BillNumber, $"Fatura iptali {bill.BillNumber}", ct);
        }
        if (wasPosted && due > 0m)
        {
            var reversalLines = VendorGLLines.HasPoLinkedLines(bill)
                ? VendorGLLines.BillWithLinesReversal(bill, due)
                : BuildHeaderReversal(bill, due);
            await _outbox.EnqueueAsync(new GLPostingRequest(
                JournalSourceType.VendorBillReversal, bill.Id, bill.BillNumber, DateTime.UtcNow.Date,
                JournalEntryType.Mahsup, $"Fatura iptali {bill.BillNumber}",
                reversalLines,
                bill.Currency, bill.ExchangeRate), ct);
        }
        _bills.Update(bill);
        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(bill);
    }

    // Unchanged PO-less / header-only reversal: prorate tax to the open portion
    // and credit the single debit account at the remaining subtotal.
    private static IReadOnlyList<GLPostingLine> BuildHeaderReversal(VendorBill bill, decimal due)
    {
        var factor = bill.Total == 0m ? 0m : due / bill.Total;
        var reversedTax = Math.Round(bill.TaxAmount * factor, 4, MidpointRounding.ToEven);
        var reversedSubtotal = due - reversedTax;
        return VendorGLLines.Bill(reversedSubtotal, reversedTax, due, bill.PurchaseOrderId is not null, reverse: true);
    }
}

public class CreateVendorPaymentHandler : IRequestHandler<CreateVendorPaymentCommand, VendorPaymentDto>
{
    private readonly IVendorPaymentRepository _payments;
    private readonly IVendorBillRepository _bills;
    private readonly IVendorRepository _vendors;
    private readonly IVendorLedgerRepository _ledger;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IVendorPaymentApplicationRepository _applications;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IGLPostingOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public CreateVendorPaymentHandler(
        IVendorPaymentRepository payments,
        IVendorBillRepository bills,
        IVendorRepository vendors,
        IVendorLedgerRepository ledger,
        IDocumentSequenceRepository sequences,
        IVendorPaymentApplicationRepository applications,
        ICurrentUserAccessor currentUser,
        IGLPostingOutbox outbox,
        IUnitOfWork uow)
    {
        _payments = payments;
        _bills = bills;
        _vendors = vendors;
        _ledger = ledger;
        _sequences = sequences;
        _applications = applications;
        _currentUser = currentUser;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<VendorPaymentDto> Handle(CreateVendorPaymentCommand c, CancellationToken ct)
    {
        // WHY: durable idempotency — a network retry/double-submit with the same OperationId replays the
        // original payment instead of consuming a new sequence + double-applying against the bill.
        if (c.OperationId is { } operationId)
        {
            var replay = await _payments.GetByOperationIdAsync(operationId, ct);
            if (replay is not null)
            {
                return VendorBillingMapper.ToDto(replay);
            }
        }
        if (c.Amount <= 0m)
        {
            throw new StockMovementValidationException("Payment amount must be positive.");
        }
        var vendor = await _vendors.GetByIdAsync(c.VendorId, ct) ?? throw new VendorNotFoundForPurchaseException();
        var now = DateTime.UtcNow;
        var paymentCurrency = c.Currency.ToUpperInvariant();

        VendorBill? autoBill = null;
        decimal autoApplyAmount = 0m;
        // An advance is a prepayment with no bill yet → never auto-applies; offset later.
        if (!c.IsAdvance && c.VendorBillId is { } billId)
        {
            autoBill = await _bills.GetByIdAsync(billId, ct) ?? throw new VendorBillNotFoundException();
            if (autoBill.VendorId != vendor.Id
                || !string.Equals(autoBill.Currency, paymentCurrency, StringComparison.OrdinalIgnoreCase)
                || autoBill.Status is VendorBillStatus.Draft or VendorBillStatus.Cancelled or VendorBillStatus.Paid)
            {
                throw new VendorPaymentBillMismatchException();
            }
            autoApplyAmount = Math.Min(Math.Round(c.Amount, 4), autoBill.AmountDue);
            if (autoApplyAmount <= 0m)
            {
                throw new VendorPaymentBillMismatchException();
            }
        }

        var seq = await _sequences.GetAsync(DocumentSequenceType.VendorPaymentNumber, ct);
        if (seq is null)
        {
            await _sequences.AddAsync(new DocumentSequence(DocumentSequenceType.VendorPaymentNumber, "VPAY", now.Year, 1, 5), ct);
            await _uow.SaveChangesAsync(ct);
        }
        var number = await _sequences.ConsumeAsync(DocumentSequenceType.VendorPaymentNumber, now, ct);

        var payment = new VendorPayment(vendor.Id, vendor.Name, number, c.PaymentDate, c.Amount,
            paymentCurrency, c.ExchangeRate, c.Method, c.IsAdvance ? null : c.VendorBillId, c.Notes, c.IsAdvance, c.OperationId);
        await _payments.AddAsync(payment, ct);

        if (autoBill is not null && autoApplyAmount > 0m)
        {
            autoBill.RecordPayment(autoApplyAmount);
            _bills.Update(autoBill);
            payment.RecordApplication(autoApplyAmount);
            var application = new VendorPaymentApplication(
                payment.Id, autoBill.Id, autoApplyAmount, _currentUser.UserId, c.Notes);
            await _applications.AddAsync(application, ct);
        }

        await VendorLedgerPoster.PostAsync(_ledger, _vendors, vendor.Id, now,
            LedgerEntryType.Debit, c.Amount, c.Currency.ToUpperInvariant(), c.ExchangeRate,
            c.IsAdvance ? LedgerSourceType.AdvanceReceived : LedgerSourceType.Payment,
            payment.Id, number,
            c.IsAdvance ? $"Tedarikçi avansı {number}" : $"Tedarikçi ödemesi {number}", ct);

        var cashKey = string.Equals(c.Method, "Cash", StringComparison.OrdinalIgnoreCase)
            ? GLPostingKey.Cash
            : GLPostingKey.Bank;
        // Advance paid: DR 159 (Verilen Sipariş Avansları) / CR cash — no bill yet, must NOT hit AP(320).
        var controlKey = c.IsAdvance ? GLPostingKey.VendorAdvancePaid : GLPostingKey.AccountsPayable;
        await _outbox.EnqueueAsync(new GLPostingRequest(
            c.IsAdvance ? JournalSourceType.VendorAdvancePaid : JournalSourceType.VendorPayment,
            payment.Id, number, now.Date,
            JournalEntryType.Tediye,
            c.IsAdvance ? $"Tedarikçi avansı {number}" : $"Tedarikçi ödemesi {number}",
            PaymentGLLines.CashMovement(cashKey, controlKey, c.Amount, cashIsDebit: false),
            c.Currency.ToUpperInvariant(), c.ExchangeRate), ct);

        payment.Post();

        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(payment);
    }
}

public class SearchVendorBillsHandler : IRequestHandler<SearchVendorBillsQuery, PagedResult<VendorBillDto>>
{
    private readonly IVendorBillRepository _bills;
    public SearchVendorBillsHandler(IVendorBillRepository bills) => _bills = bills;

    public async Task<PagedResult<VendorBillDto>> Handle(SearchVendorBillsQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _bills.SearchAsync(q.VendorId, q.Status, page, pageSize, ct);
        return new PagedResult<VendorBillDto>
        {
            Items = items.Select(VendorBillingMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetVendorBillByIdHandler : IRequestHandler<GetVendorBillByIdQuery, VendorBillDto?>
{
    private readonly IVendorBillRepository _bills;
    public GetVendorBillByIdHandler(IVendorBillRepository bills) => _bills = bills;

    public async Task<VendorBillDto?> Handle(GetVendorBillByIdQuery q, CancellationToken ct)
    {
        var bill = await _bills.GetByIdAsync(q.Id, ct)
            ?? throw new VendorBillNotFoundException(q.Id);
        return VendorBillingMapper.ToDto(bill);
    }
}

public class GetVendorAgingHandler : IRequestHandler<GetVendorAgingQuery, IReadOnlyList<VendorAgingRowDto>>
{
    private readonly IVendorBillRepository _bills;
    public GetVendorAgingHandler(IVendorBillRepository bills) => _bills = bills;

    public async Task<IReadOnlyList<VendorAgingRowDto>> Handle(GetVendorAgingQuery q, CancellationToken ct)
    {
        var asOf = (q.AsOfUtc ?? DateTime.UtcNow).Date;
        var rows = await _bills.GetAgingBucketsAsync(asOf, ct);
        return rows.Select(r => new VendorAgingRowDto(
            r.VendorId, r.VendorName, r.Currency,
            r.Current, r.Days1To30, r.Days31To60, r.Days61To90, r.DaysOver90,
            r.Current + r.Days1To30 + r.Days31To60 + r.Days61To90 + r.DaysOver90)).ToList();
    }
}

public class SearchVendorPaymentsHandler : IRequestHandler<SearchVendorPaymentsQuery, PagedResult<VendorPaymentDto>>
{
    private readonly IVendorPaymentRepository _payments;
    public SearchVendorPaymentsHandler(IVendorPaymentRepository payments) => _payments = payments;

    public async Task<PagedResult<VendorPaymentDto>> Handle(SearchVendorPaymentsQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _payments.SearchAsync(q.VendorId, page, pageSize, ct);
        return new PagedResult<VendorPaymentDto>
        {
            Items = items.Select(VendorBillingMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetVendorPaymentByIdHandler : IRequestHandler<GetVendorPaymentByIdQuery, VendorPaymentDto?>
{
    private readonly IVendorPaymentRepository _payments;
    public GetVendorPaymentByIdHandler(IVendorPaymentRepository payments) => _payments = payments;

    public async Task<VendorPaymentDto?> Handle(GetVendorPaymentByIdQuery q, CancellationToken ct)
    {
        var entity = await _payments.GetByIdAsync(q.Id, ct);
        return entity is null ? null : VendorBillingMapper.ToDto(entity);
    }
}

public class UpdateVendorBillHandler : IRequestHandler<UpdateVendorBillCommand, VendorBillDto>
{
    private readonly IVendorBillRepository _bills;
    private readonly IProductRepository _products;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IUnitOfWork _uow;

    public UpdateVendorBillHandler(
        IVendorBillRepository bills,
        IProductRepository products,
        IPurchaseOrderRepository orders,
        IUnitOfWork uow)
    {
        _bills = bills;
        _products = products;
        _orders = orders;
        _uow = uow;
    }

    public async Task<VendorBillDto> Handle(UpdateVendorBillCommand c, CancellationToken ct)
    {
        var bill = await _bills.GetByIdAsync(c.Id, ct) ?? throw new VendorBillNotFoundException();
        if (await _bills.BillNumberExistsAsync(bill.VendorId, c.BillNumber.Trim(), bill.Id, ct))
        {
            throw new DuplicateVendorBillNumberException();
        }
        bill.UpdateDraft(c.BillNumber.Trim(), c.BillDate, c.DueDate, c.Currency.ToUpperInvariant(),
            c.ExchangeRate, c.Subtotal, c.TaxAmount, c.PurchaseOrderId, c.Notes);

        if (c.Lines is not null)
        {
            var lines = c.Lines.Count == 0
                ? new List<VendorBillLine>()
                : await VendorBillLineFactory.BuildAsync(c.Lines, c.PurchaseOrderId, _products, _orders, ct);
            bill.ReplaceLines(lines);
        }

        _bills.Update(bill);
        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(bill);
    }
}

public class UpdateVendorPaymentHandler : IRequestHandler<UpdateVendorPaymentCommand, VendorPaymentDto>
{
    private readonly IVendorPaymentRepository _payments;
    private readonly IUnitOfWork _uow;

    public UpdateVendorPaymentHandler(IVendorPaymentRepository payments, IUnitOfWork uow)
    {
        _payments = payments;
        _uow = uow;
    }

    public async Task<VendorPaymentDto> Handle(UpdateVendorPaymentCommand c, CancellationToken ct)
    {
        if (c.Amount <= 0m)
        {
            throw new StockMovementValidationException("Payment amount must be positive.");
        }
        var payment = await _payments.GetByIdAsync(c.Id, ct) ?? throw new VendorPaymentApplicationNotFoundException();
        if (payment.IsVoided || payment.IsPosted)
        {
            throw new VendorPaymentImmutableException();
        }
        payment.UpdateDraft(c.PaymentDate, c.Amount, c.Currency.ToUpperInvariant(), c.ExchangeRate, c.Method, c.Notes);
        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(payment);
    }
}

public class VoidVendorPaymentHandler : IRequestHandler<VoidVendorPaymentCommand, VendorPaymentDto>
{
    private readonly IVendorPaymentRepository _payments;
    private readonly IVendorBillRepository _bills;
    private readonly IVendorPaymentApplicationRepository _applications;
    private readonly IVendorLedgerRepository _ledger;
    private readonly IVendorRepository _vendors;
    private readonly IGLPostingOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public VoidVendorPaymentHandler(
        IVendorPaymentRepository payments,
        IVendorBillRepository bills,
        IVendorPaymentApplicationRepository applications,
        IVendorLedgerRepository ledger,
        IVendorRepository vendors,
        IGLPostingOutbox outbox,
        IUnitOfWork uow)
    {
        _payments = payments;
        _bills = bills;
        _applications = applications;
        _ledger = ledger;
        _vendors = vendors;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<VendorPaymentDto> Handle(VoidVendorPaymentCommand c, CancellationToken ct)
    {
        var payment = await _payments.GetByIdAsync(c.Id, ct) ?? throw new VendorPaymentApplicationNotFoundException();
        var applications = await _applications.GetByVendorPaymentAsync(payment.Id, ct);
        foreach (var app in applications)
        {
            var bill = await _bills.GetByIdAsync(app.VendorBillId, ct);
            if (bill is not null)
            {
                bill.ReverseRecordedPayment(app.AppliedAmount);
                _bills.Update(bill);
            }
            // WHY: an advance offset booked DR 320 / CR 159 (VendorAdvanceApplied); voiding must reverse it (DR 159 / CR 320).
            if (payment.IsAdvance)
            {
                await _outbox.EnqueueAsync(new GLPostingRequest(
                    JournalSourceType.VendorAdvanceAppliedReversal, app.Id, payment.PaymentNumber, DateTime.UtcNow.Date,
                    JournalEntryType.Mahsup, $"Tedarikçi avans mahsup iptali {payment.PaymentNumber}",
                    new[]
                    {
                        new GLPostingLine(GLPostingKey.VendorAdvancePaid, app.AppliedAmount, 0m),
                        new GLPostingLine(GLPostingKey.AccountsPayable, 0m, app.AppliedAmount),
                    },
                    payment.Currency, payment.ExchangeRate), ct);
            }
            payment.ReverseApplication(app.AppliedAmount);
            _applications.Remove(app);
        }
        payment.Void(c.Reason);

        await VendorLedgerPoster.PostAsync(_ledger, _vendors, payment.VendorId, DateTime.UtcNow,
            LedgerEntryType.Credit, payment.Amount, payment.Currency, payment.ExchangeRate,
            LedgerSourceType.PaymentReversal, payment.Id, payment.PaymentNumber,
            $"Tedarikçi ödeme iptali {payment.PaymentNumber}", ct);

        var cashKey = string.Equals(payment.Method, "Cash", StringComparison.OrdinalIgnoreCase)
            ? GLPostingKey.Cash
            : GLPostingKey.Bank;
        // WHY: an advance was booked DR 159 (VendorAdvancePaid) / CR cash — reverse it back to 159, not AP(320).
        var controlKey = payment.IsAdvance ? GLPostingKey.VendorAdvancePaid : GLPostingKey.AccountsPayable;
        var reversalSource = payment.IsAdvance
            ? JournalSourceType.VendorAdvancePaidReversal
            : JournalSourceType.VendorPaymentReversal;
        await _outbox.EnqueueAsync(new GLPostingRequest(
            reversalSource, payment.Id, payment.PaymentNumber, DateTime.UtcNow.Date,
            JournalEntryType.Mahsup, $"Tedarikçi ödeme iptali {payment.PaymentNumber}",
            PaymentGLLines.CashMovement(cashKey, controlKey, payment.Amount, cashIsDebit: true),
            payment.Currency, payment.ExchangeRate), ct);

        _payments.Update(payment);
        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(payment);
    }
}

public class ApplyVendorPaymentHandler : IRequestHandler<ApplyVendorPaymentCommand, VendorPaymentApplicationDto>
{
    private readonly IVendorPaymentRepository _payments;
    private readonly IVendorBillRepository _bills;
    private readonly IVendorPaymentApplicationRepository _applications;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;

    public ApplyVendorPaymentHandler(
        IVendorPaymentRepository payments,
        IVendorBillRepository bills,
        IVendorPaymentApplicationRepository applications,
        ICurrentUserAccessor currentUser,
        IUnitOfWork uow)
    {
        _payments = payments;
        _bills = bills;
        _applications = applications;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<VendorPaymentApplicationDto> Handle(ApplyVendorPaymentCommand c, CancellationToken ct)
    {
        if (c.Amount <= 0m)
        {
            throw new VendorPaymentOverApplicationException();
        }
        var payment = await _payments.GetByIdAsync(c.VendorPaymentId, ct)
            ?? throw new VendorPaymentApplicationNotFoundException();
        var bill = await _bills.GetByIdAsync(c.VendorBillId, ct) ?? throw new VendorBillNotFoundException();

        var existing = await _applications.GetByPaymentAndBillAsync(payment.Id, bill.Id, ct);
        if (existing is not null)
        {
            return VendorBillingMapper.ToDto(existing, payment.PaymentNumber, bill.BillNumber);
        }

        if (payment.IsVoided)
        {
            throw new VendorPaymentAlreadyVoidedException();
        }
        if (payment.VendorId != bill.VendorId
            || !string.Equals(payment.Currency, bill.Currency, StringComparison.OrdinalIgnoreCase)
            || bill.Status is VendorBillStatus.Draft or VendorBillStatus.Cancelled or VendorBillStatus.Paid)
        {
            throw new VendorPaymentBillMismatchException();
        }

        var amount = Math.Round(c.Amount, 4);
        if (amount > payment.UnappliedAmount + 0.0001m)
        {
            throw new VendorPaymentOverApplicationException();
        }
        if (amount > bill.AmountDue + 0.0001m)
        {
            throw new VendorPaymentOverApplicationException();
        }

        var application = new VendorPaymentApplication(payment.Id, bill.Id, amount, _currentUser.UserId, c.Notes);
        await _applications.AddAsync(application, ct);

        payment.RecordApplication(amount);
        _payments.Update(payment);

        bill.RecordPayment(amount);
        _bills.Update(bill);

        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(application, payment.PaymentNumber, bill.BillNumber);
    }
}

public class OffsetVendorAdvanceHandler : IRequestHandler<OffsetVendorAdvanceCommand, VendorPaymentApplicationDto>
{
    private readonly IVendorPaymentRepository _payments;
    private readonly IVendorBillRepository _bills;
    private readonly IVendorPaymentApplicationRepository _applications;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IGLPostingOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public OffsetVendorAdvanceHandler(
        IVendorPaymentRepository payments,
        IVendorBillRepository bills,
        IVendorPaymentApplicationRepository applications,
        ICurrentUserAccessor currentUser,
        IGLPostingOutbox outbox,
        IUnitOfWork uow)
    {
        _payments = payments;
        _bills = bills;
        _applications = applications;
        _currentUser = currentUser;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<VendorPaymentApplicationDto> Handle(OffsetVendorAdvanceCommand c, CancellationToken ct)
    {
        if (c.Amount <= 0m)
        {
            throw new VendorPaymentOverApplicationException();
        }
        var payment = await _payments.GetByIdAsync(c.VendorPaymentId, ct)
            ?? throw new VendorPaymentApplicationNotFoundException();
        if (!payment.IsAdvance)
        {
            throw new VendorPaymentBillMismatchException();
        }
        var bill = await _bills.GetByIdAsync(c.VendorBillId, ct) ?? throw new VendorBillNotFoundException();

        var existing = await _applications.GetByPaymentAndBillAsync(payment.Id, bill.Id, ct);
        if (existing is not null)
        {
            return VendorBillingMapper.ToDto(existing, payment.PaymentNumber, bill.BillNumber);
        }

        if (payment.IsVoided)
        {
            throw new VendorPaymentAlreadyVoidedException();
        }
        if (payment.VendorId != bill.VendorId
            || !string.Equals(payment.Currency, bill.Currency, StringComparison.OrdinalIgnoreCase)
            || bill.Status is VendorBillStatus.Draft or VendorBillStatus.Cancelled or VendorBillStatus.Paid)
        {
            throw new VendorPaymentBillMismatchException();
        }

        var amount = Math.Round(c.Amount, 4);
        // Over-offset cap: cannot exceed the unapplied advance balance nor the bill due.
        if (amount > payment.UnappliedAmount + 0.0001m || amount > bill.AmountDue + 0.0001m)
        {
            throw new VendorPaymentOverApplicationException();
        }

        var application = new VendorPaymentApplication(payment.Id, bill.Id, amount, _currentUser.UserId, c.Notes);
        await _applications.AddAsync(application, ct);
        payment.RecordApplication(amount);
        _payments.Update(payment);
        bill.RecordPayment(amount);
        _bills.Update(bill);

        // Offset (mahsup): consume the prepayment in 159 against AP(320). Keyed on the
        // application id (dedup), posted at the advance's ExchangeRate.
        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.VendorAdvanceApplied, application.Id, payment.PaymentNumber, DateTime.UtcNow.Date,
            JournalEntryType.Mahsup, $"Tedarikçi avans mahsubu {payment.PaymentNumber}",
            new[]
            {
                new GLPostingLine(GLPostingKey.AccountsPayable, amount, 0m),
                new GLPostingLine(GLPostingKey.VendorAdvancePaid, 0m, amount),
            },
            payment.Currency, payment.ExchangeRate), ct);

        await _uow.SaveChangesAsync(ct);
        return VendorBillingMapper.ToDto(application, payment.PaymentNumber, bill.BillNumber);
    }
}

public class GetVendorBillApplicationsHandler : IRequestHandler<GetVendorBillApplicationsQuery, IReadOnlyList<VendorPaymentApplicationDto>>
{
    private readonly IVendorPaymentApplicationRepository _applications;
    private readonly IVendorPaymentRepository _payments;
    private readonly IVendorBillRepository _bills;

    public GetVendorBillApplicationsHandler(
        IVendorPaymentApplicationRepository applications,
        IVendorPaymentRepository payments,
        IVendorBillRepository bills)
    {
        _applications = applications;
        _payments = payments;
        _bills = bills;
    }

    public async Task<IReadOnlyList<VendorPaymentApplicationDto>> Handle(GetVendorBillApplicationsQuery q, CancellationToken ct)
    {
        var apps = await _applications.GetByVendorBillAsync(q.VendorBillId, ct);
        if (apps.Count == 0) return Array.Empty<VendorPaymentApplicationDto>();
        var bill = await _bills.GetByIdAsync(q.VendorBillId, ct);
        var billNumber = bill?.BillNumber ?? string.Empty;
        var payments = await _payments.GetByIdsAsync(apps.Select(a => a.VendorPaymentId).Distinct(), ct);
        var paymentNumbers = payments.ToDictionary(p => p.Id, p => p.PaymentNumber);
        return apps
            .Select(a => VendorBillingMapper.ToDto(a, paymentNumbers.GetValueOrDefault(a.VendorPaymentId, string.Empty), billNumber))
            .ToList();
    }
}

public class GetVendorPaymentApplicationsHandler : IRequestHandler<GetVendorPaymentApplicationsQuery, IReadOnlyList<VendorPaymentApplicationDto>>
{
    private readonly IVendorPaymentApplicationRepository _applications;
    private readonly IVendorPaymentRepository _payments;
    private readonly IVendorBillRepository _bills;

    public GetVendorPaymentApplicationsHandler(
        IVendorPaymentApplicationRepository applications,
        IVendorPaymentRepository payments,
        IVendorBillRepository bills)
    {
        _applications = applications;
        _payments = payments;
        _bills = bills;
    }

    public async Task<IReadOnlyList<VendorPaymentApplicationDto>> Handle(GetVendorPaymentApplicationsQuery q, CancellationToken ct)
    {
        var apps = await _applications.GetByVendorPaymentAsync(q.VendorPaymentId, ct);
        if (apps.Count == 0) return Array.Empty<VendorPaymentApplicationDto>();
        var payment = await _payments.GetByIdAsync(q.VendorPaymentId, ct);
        var paymentNumber = payment?.PaymentNumber ?? string.Empty;
        var bills = await _bills.GetByIdsAsync(apps.Select(a => a.VendorBillId).Distinct(), ct);
        var billNumbers = bills.ToDictionary(b => b.Id, b => b.BillNumber);
        return apps
            .Select(a => VendorBillingMapper.ToDto(a, paymentNumber, billNumbers.GetValueOrDefault(a.VendorBillId, string.Empty)))
            .ToList();
    }
}

public class GetThreeWayMatchHandler : IRequestHandler<GetThreeWayMatchQuery, IReadOnlyList<ThreeWayMatchRowDto>>
{
    private readonly IThreeWayMatchReader _reader;
    public GetThreeWayMatchHandler(IThreeWayMatchReader reader) => _reader = reader;

    public async Task<IReadOnlyList<ThreeWayMatchRowDto>> Handle(GetThreeWayMatchQuery q, CancellationToken ct)
    {
        var rows = await _reader.GetMismatchesAsync(q.VendorId, q.FromUtc, q.ToUtc, ct);
        return rows.Select(r => new ThreeWayMatchRowDto(
            r.PurchaseOrderId, r.PoNumber, r.VendorId, r.VendorName, r.Currency,
            r.ProductId, r.ProductSku, r.ProductName,
            r.ExpectedQty, r.ReceivedQty, r.BilledQty,
            r.ExpectedAmount, r.BilledAmount, r.Discrepancies)).ToList();
    }
}
