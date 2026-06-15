using CoreAlign.Application.Accounting.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Accounting;

public class GLPostingServiceExtraTests
{
    private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
    private readonly IGLAccountRepository _accounts = Substitute.For<IGLAccountRepository>();
    private readonly IGLPostingMappingRepository _mappings = Substitute.For<IGLPostingMappingRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly List<GLAccount> _chart = new();
    private readonly GLPostingService _sut;

    public GLPostingServiceExtraTests()
    {
        _sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));
        _accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_chart);
        _mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
        _sut = new GLPostingService(_journals, _accounts, _mappings, _sequences, _periods);
    }

    private void MapAccount(string code, bool postable = true, bool active = true)
    {
        var a = new GLAccount(code, $"Account {code}", AccountType.Asset, isPostable: postable);
        if (!active)
        {
            a.Deactivate();
        }
        _chart.Add(a);
    }

    private static GLPostingRequest PaymentRequest(decimal amount = 500m) => new(
        JournalSourceType.CustomerPayment,
        Guid.NewGuid(),
        "PAY-1",
        new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        JournalEntryType.Tahsil,
        "Tahsilat PAY-1",
        PaymentGLLines.CashMovement(GLPostingKey.Cash, GLPostingKey.AccountsReceivable, amount, cashIsDebit: true));

    [Fact]
    public async Task Returns_SkippedEmpty_when_no_lines_supplied()
    {
        var req = new GLPostingRequest(
            JournalSourceType.SalesInvoice,
            Guid.NewGuid(),
            "INV-EMPTY",
            DateTime.UtcNow,
            JournalEntryType.Mahsup,
            "no lines",
            Array.Empty<GLPostingLine>());

        var r = await _sut.PostAsync(req, default);

        r.Should().Be(GLPostingResult.SkippedEmpty);
        await _journals.DidNotReceive().AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_SkippedClosedPeriod_when_target_period_is_closed()
    {
        MapAccount("100");
        MapAccount("120");
        var period = new AccountingPeriod(2026, 6);
        period.Close(Guid.NewGuid(), null);
        _periods.GetByDateAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(period);

        var r = await _sut.PostAsync(PaymentRequest(), default);

        r.Should().Be(GLPostingResult.SkippedClosedPeriod);
        await _journals.DidNotReceive().AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_SkippedUnmapped_when_target_account_is_inactive()
    {
        MapAccount("100");
        MapAccount("120", active: false);

        var r = await _sut.PostAsync(PaymentRequest(), default);

        r.Should().Be(GLPostingResult.SkippedUnmapped);
        await _journals.DidNotReceive().AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tenant_override_takes_priority_over_default_account_code()
    {
        MapAccount("100");
        MapAccount("999");
        _mappings.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new List<GLPostingMapping> { new(GLPostingKey.AccountsReceivable, "999") });

        await _sut.PostAsync(PaymentRequest(amount: 200m), default);

        await _journals.Received(1).AddAsync(
            Arg.Is<JournalEntry>(j =>
                j.Status == JournalEntryStatus.Posted &&
                j.Lines.Any(l => l.AccountCode == "999")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Zero_amount_lines_are_dropped_and_unbalanced_residual_falls_to_SkippedEmpty()
    {
        MapAccount("100");
        MapAccount("120");
        var req = new GLPostingRequest(
            JournalSourceType.CustomerPayment, Guid.NewGuid(), "PAY-Z",
            DateTime.UtcNow,
            JournalEntryType.Tahsil, "zero", new[]
            {
                new GLPostingLine(GLPostingKey.Cash, 0m, 0m),
            });

        var r = await _sut.PostAsync(req, default);

        r.Should().Be(GLPostingResult.SkippedEmpty);
    }

    [Fact]
    public async Task Posts_payment_with_cash_debited_and_AR_credited_for_receipt()
    {
        MapAccount("100");
        MapAccount("120");

        await _sut.PostAsync(PaymentRequest(amount: 750m), default);

        await _journals.Received(1).AddAsync(
            Arg.Is<JournalEntry>(j =>
                j.TotalDebit == 750m && j.TotalCredit == 750m &&
                j.Lines.Any(l => l.AccountCode == "100" && l.Debit == 750m) &&
                j.Lines.Any(l => l.AccountCode == "120" && l.Credit == 750m)),
            Arg.Any<CancellationToken>());
    }
}
