using CoreAlign.Application.Accounting.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.BugHuntA;

/// <summary>
/// HUNTER A — money-conservation bug reproductions. Each [Fact] is RED on current
/// code and pins a concrete misstatement: GL revenue understated by withholding,
/// and a credit note that does not exactly reverse the origin invoice when the
/// origin carried a header discount.
/// </summary>
public class MoneyConservationBugTests
{
    private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
    private readonly IGLAccountRepository _accounts = Substitute.For<IGLAccountRepository>();
    private readonly IGLPostingMappingRepository _mappings = Substitute.For<IGLPostingMappingRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly List<GLAccount> _chart = new();
    private readonly GLPostingService _sut;

    public MoneyConservationBugTests()
    {
        _sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));
        _accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_chart);
        _mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
        foreach (var code in new[] { "120", "600", "391", "100", "102", "320", "191", "153", "621", "322", "632", "689", "193", "360" })
        {
            _chart.Add(new GLAccount(code, $"Account {code}", AccountType.Asset, isPostable: true));
        }
        _sut = new GLPostingService(_journals, _accounts, _mappings, _sequences, _periods);
    }

    // Mirrors the internal SalesGLLines.Build used by InvoiceIssuedGLHandler:
    // revenue is the taxable base; the unpaid withholding is debited to a
    // withholding-receivable control account so DR(AR + WH) == CR(Revenue + VAT).
    private static IReadOnlyList<GLPostingLine> SalesLines(decimal revenue, decimal tax, decimal withholding, bool reverse)
    {
        revenue = Math.Max(0m, revenue);
        withholding = Math.Max(0m, withholding);
        var receivable = Math.Max(0m, revenue + tax - withholding);
        return reverse
            ? new[]
            {
                new GLPostingLine(GLPostingKey.SalesRevenue, revenue, 0m),
                new GLPostingLine(GLPostingKey.OutputVat, tax, 0m),
                new GLPostingLine(GLPostingKey.AccountsReceivable, 0m, receivable),
                new GLPostingLine(GLPostingKey.WithholdingReceivable, 0m, withholding),
            }
            : new[]
            {
                new GLPostingLine(GLPostingKey.AccountsReceivable, receivable, 0m),
                new GLPostingLine(GLPostingKey.WithholdingReceivable, withholding, 0m),
                new GLPostingLine(GLPostingKey.SalesRevenue, 0m, revenue),
                new GLPostingLine(GLPostingKey.OutputVat, 0m, tax),
            };
    }

    private async Task<JournalEntry> CapturePostedAsync(decimal revenue, decimal tax, decimal withholding)
    {
        JournalEntry? captured = null;
        await _journals.AddAsync(Arg.Do<JournalEntry>(j => captured = j), Arg.Any<CancellationToken>());
        var request = new GLPostingRequest(
            JournalSourceType.SalesInvoice,
            Guid.NewGuid(),
            "INV-WH",
            DateTime.UtcNow.Date,
            JournalEntryType.Mahsup,
            "Satış faturası",
            SalesLines(revenue, tax, withholding, reverse: false));
        var result = await _sut.PostAsync(request, default);
        result.Should().Be(GLPostingResult.Posted);
        captured.Should().NotBeNull();
        return captured!;
    }

    private static InvoiceLine WithholdingLine(
        decimal qty, decimal unitPrice, decimal vatPct, decimal withholdingPct)
    {
        var line = new InvoiceLine("SKU-1", "Hizmet", null, qty, unitPrice);
        line.ApplyPricing(
            quantity: qty,
            unitPrice: unitPrice,
            lineDiscountPercent: 0m,
            lineDiscountAmount: 0m,
            taxRatePercent: vatPct,
            taxRateId: null,
            isTaxInclusive: false,
            withholdingRatePercent: withholdingPct,
            uomId: null,
            uomCode: null,
            description: null,
            revenueAccountCode: null,
            costCenter: null,
            project: null,
            originOrderLineId: null);
        return line;
    }

    [Fact]
    public async Task A1_withholding_invoice_gl_understates_revenue_by_the_withholding_amount()
    {
        // 1000 net, 20% VAT = 200. Withholding base is (net + VAT) = 1200, 10% = 120.
        // Real economics: Revenue 1000, Output VAT 200, AR (collectible) 1080,
        // Withholding-receivable 120. Σ DR (1080 + 120) = Σ CR (1000 + 200) = 1200.
        var invoice = new Invoice("INV-WH", Guid.NewGuid(), "Müşteri", "TRY");
        invoice.ReplaceLines(new[] { WithholdingLine(qty: 10m, unitPrice: 100m, vatPct: 20m, withholdingPct: 10m) });

        invoice.TaxableTotal.Should().Be(1000m);
        invoice.TaxTotal.Should().Be(200m);
        invoice.WithholdingTotal.Should().Be(120m);
        invoice.Total.Should().Be(1080m); // AR actually collectible from customer

        var entry = await CapturePostedAsync(invoice.TaxableTotal, invoice.TaxTotal, invoice.WithholdingTotal);

        var revenueCredit = entry.Lines.Single(l => l.AccountCode == "600").Credit;

        // Revenue must equal the taxable base (1000), not Total-minus-VAT (1080-200=880)
        // which nets out the withholding. The 120 withholding is booked to the
        // withholding-receivable control account (193) so the entry still balances.
        revenueCredit.Should().Be(1000m,
            "sales revenue must equal the taxable base, not Total-minus-VAT which nets out the withholding");

        var withholdingDebit = entry.Lines.Single(l => l.AccountCode == "193").Debit;
        withholdingDebit.Should().Be(120m,
            "withholding tax must be booked to a withholding-receivable account, not dropped");

        // AR control debited only for what the customer actually owes (1080).
        entry.Lines.Single(l => l.AccountCode == "120").Debit.Should().Be(1080m);

        // The entry still balances: DR(AR 1080 + WH 120) == CR(Revenue 1000 + VAT 200).
        entry.TotalDebit.Should().Be(entry.TotalCredit);
    }

    [Fact]
    public void A2_credit_note_does_not_reverse_origin_when_origin_has_header_discount()
    {
        // Origin: 1000 net line, 18% VAT = 180. Header discount 10% → taxable 900?
        // The origin applies a 10% header discount; its Total reflects that discount.
        var origin = new Invoice("INV-1", Guid.NewGuid(), "Müşteri", "TRY");
        var originLine = new InvoiceLine("SKU-1", "Ürün", null, 10m, 100m);
        originLine.ApplyPricing(
            quantity: 10m, unitPrice: 100m,
            lineDiscountPercent: 0m, lineDiscountAmount: 0m,
            taxRatePercent: 18m, taxRateId: null, isTaxInclusive: false,
            withholdingRatePercent: 0m,
            uomId: null, uomCode: null, description: null,
            revenueAccountCode: null, costCenter: null, project: null,
            originOrderLineId: null);
        origin.UpdateDetails(
            issueDate: DateTime.UtcNow, dueDate: DateTime.UtcNow.AddDays(30),
            postingDate: DateTime.UtcNow.Date, exchangeRate: 1m,
            paymentTermsId: null, paymentTermsNetDaysSnapshot: null,
            headerDiscountPercent: 10m, headerDiscountAmount: 0m,
            shippingCost: 0m, roundingAdjustment: 0m,
            internalNotes: null, publicNotes: null, termsAndConditions: null, notes: null);
        origin.ReplaceLines(new[] { originLine });
        origin.Issue("INV-1");

        // Full-quantity credit note for the whole invoice.
        var creditLine = new InvoiceLine("SKU-1", "Ürün", null, 10m, 100m);
        creditLine.ApplyPricing(
            quantity: 10m, unitPrice: 100m,
            lineDiscountPercent: 0m, lineDiscountAmount: 0m,
            taxRatePercent: 18m, taxRateId: null, isTaxInclusive: false,
            withholdingRatePercent: 0m,
            uomId: null, uomCode: null, description: null,
            revenueAccountCode: null, costCenter: null, project: null,
            originOrderLineId: originLine.Id);

        var creditNote = Invoice.IssueCreditNote(
            origin, "CN-1", DateTime.UtcNow, new[] { creditLine },
            reason: "Full return", approvedByUserId: null, returnRequestId: Guid.NewGuid());

        // BUG: the credit note rebuilds only the line, dropping the origin's header
        // discount, so its Total (full gross) exceeds the origin's discounted Total.
        // The reversing GL entry therefore over-credits AR/revenue versus the
        // original posting — the customer's balance does not net to zero.
        creditNote.Total.Should().Be(origin.Total,
            "a full credit note must reverse exactly what the origin invoice posted, including header discount");
    }
}
