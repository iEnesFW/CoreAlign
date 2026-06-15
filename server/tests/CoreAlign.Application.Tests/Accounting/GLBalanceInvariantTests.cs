using CoreAlign.Application.Accounting.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Accounting;

/// <summary>
/// Rule 16 GL invariant: every posted JournalEntry must have Σ debits == Σ credits.
/// These tests drive the real <see cref="GLPostingService"/> for a sales invoice,
/// a reversing credit note, a vendor payment and a customer receipt — the exact
/// posting shapes the sub-ledger event handlers emit — and assert each booked
/// entry balances exactly, including foreign-currency residual handling.
/// </summary>
public class GLBalanceInvariantTests
{
    private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
    private readonly IGLAccountRepository _accounts = Substitute.For<IGLAccountRepository>();
    private readonly IGLPostingMappingRepository _mappings = Substitute.For<IGLPostingMappingRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly List<GLAccount> _chart = new();
    private readonly GLPostingService _sut;

    public GLBalanceInvariantTests()
    {
        _sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));
        _accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_chart);
        _mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
        MapStandardChart();
        _sut = new GLPostingService(_journals, _accounts, _mappings, _sequences, _periods);
    }

    private void MapStandardChart()
    {
        foreach (var code in new[] { "120", "600", "391", "100", "102", "320", "191", "153", "621", "322", "632", "689" })
        {
            _chart.Add(new GLAccount(code, $"Account {code}", AccountType.Asset, isPostable: true));
        }
    }

    private async Task<JournalEntry> CapturePostedAsync(GLPostingRequest request)
    {
        JournalEntry? captured = null;
        await _journals.AddAsync(Arg.Do<JournalEntry>(j => captured = j), Arg.Any<CancellationToken>());

        var result = await _sut.PostAsync(request, default);

        result.Should().Be(GLPostingResult.Posted);
        captured.Should().NotBeNull();
        return captured!;
    }

    private static void AssertBalanced(JournalEntry entry)
    {
        entry.Status.Should().Be(JournalEntryStatus.Posted);
        entry.TotalDebit.Should().Be(entry.TotalCredit);
        entry.Lines.Sum(l => l.Debit).Should().Be(entry.Lines.Sum(l => l.Credit));
    }

    private static GLPostingRequest SalesInvoiceRequest(decimal total, decimal tax, bool reverse) => new(
        reverse ? JournalSourceType.SalesInvoiceReversal : JournalSourceType.SalesInvoice,
        Guid.NewGuid(),
        reverse ? "CN-1" : "INV-1",
        DateTime.UtcNow.Date,
        JournalEntryType.Mahsup,
        reverse ? "İade faturası" : "Satış faturası",
        SalesLines(total, tax, reverse));

    private static IReadOnlyList<GLPostingLine> SalesLines(decimal total, decimal tax, bool reverse)
    {
        var revenue = Math.Max(0m, total - tax);
        return reverse
            ? new[]
            {
                new GLPostingLine(GLPostingKey.SalesRevenue, revenue, 0m),
                new GLPostingLine(GLPostingKey.OutputVat, tax, 0m),
                new GLPostingLine(GLPostingKey.AccountsReceivable, 0m, total),
            }
            : new[]
            {
                new GLPostingLine(GLPostingKey.AccountsReceivable, total, 0m),
                new GLPostingLine(GLPostingKey.SalesRevenue, 0m, revenue),
                new GLPostingLine(GLPostingKey.OutputVat, 0m, tax),
            };
    }

    [Fact]
    public async Task Sales_invoice_posting_is_balanced()
    {
        var entry = await CapturePostedAsync(SalesInvoiceRequest(1180m, 180m, reverse: false));

        AssertBalanced(entry);
        entry.TotalDebit.Should().Be(1180m);
    }

    [Fact]
    public async Task Credit_note_reversing_posting_is_balanced()
    {
        var entry = await CapturePostedAsync(SalesInvoiceRequest(1180m, 180m, reverse: true));

        AssertBalanced(entry);
        // Reversal books AR on the credit side for the full gross.
        entry.Lines.Single(l => l.AccountCode == "120").Credit.Should().Be(1180m);
    }

    [Fact]
    public async Task Vendor_payment_posting_is_balanced()
    {
        var request = new GLPostingRequest(
            JournalSourceType.VendorPayment,
            Guid.NewGuid(),
            "VP-1",
            DateTime.UtcNow.Date,
            JournalEntryType.Tediye,
            "Tedarikçi ödemesi",
            PaymentGLLines.CashMovement(GLPostingKey.Bank, GLPostingKey.AccountsPayable, 2540.55m, cashIsDebit: false));

        var entry = await CapturePostedAsync(request);

        AssertBalanced(entry);
        entry.Lines.Single(l => l.AccountCode == "320").Debit.Should().Be(2540.55m);
        entry.Lines.Single(l => l.AccountCode == "102").Credit.Should().Be(2540.55m);
    }

    [Fact]
    public async Task Customer_receipt_posting_is_balanced()
    {
        var request = new GLPostingRequest(
            JournalSourceType.CustomerPayment,
            Guid.NewGuid(),
            "CP-1",
            DateTime.UtcNow.Date,
            JournalEntryType.Tahsil,
            "Tahsilat",
            PaymentGLLines.CashMovement(GLPostingKey.Cash, GLPostingKey.AccountsReceivable, 999.99m, cashIsDebit: true));

        var entry = await CapturePostedAsync(request);

        AssertBalanced(entry);
        entry.Lines.Single(l => l.AccountCode == "100").Debit.Should().Be(999.99m);
    }

    [Fact]
    public async Task Foreign_currency_sales_posting_balances_after_residual_correction()
    {
        // A rate that produces a sub-cent per-line residual must be folded back so
        // the entry still balances exactly in base currency.
        var request = SalesInvoiceRequest(100.005m, 15.255m, reverse: false) with
        {
            Currency = "USD",
            ExchangeRate = 33.3333m,
        };

        var entry = await CapturePostedAsync(request);

        AssertBalanced(entry);
        entry.Lines.Should().OnlyContain(l => l.Currency == "TRY");
    }

    [Fact]
    public async Task Unbalanced_input_lines_never_reach_a_posted_entry()
    {
        // A deliberately lopsided basket (debit 100, credit 90) cannot balance —
        // the service must not produce a Posted journal entry from it.
        var request = new GLPostingRequest(
            JournalSourceType.SalesInvoice,
            Guid.NewGuid(),
            "INV-BAD",
            DateTime.UtcNow.Date,
            JournalEntryType.Mahsup,
            "broken",
            new[]
            {
                new GLPostingLine(GLPostingKey.AccountsReceivable, 100m, 0m),
                new GLPostingLine(GLPostingKey.SalesRevenue, 0m, 90m),
            });

        Func<Task> act = () => _sut.PostAsync(request, default);

        await act.Should().ThrowAsync<Domain.Exceptions.JournalEntryNotBalancedException>();
        await _journals.DidNotReceive().AddAsync(
            Arg.Is<JournalEntry>(j => j.Status == JournalEntryStatus.Posted), Arg.Any<CancellationToken>());
    }
}
