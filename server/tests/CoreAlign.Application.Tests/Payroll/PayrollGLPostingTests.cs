using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Payroll.GL;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tests.Payroll;

internal static class PayrollGLTestData
{
    public static PayrollRunTotals WorkedExample() => new(
        TotalGross: 60000m,
        TotalSgkEmployee: 8400m,
        TotalSgkEmployer: 12300m,
        TotalUnemploymentEmployee: 600m,
        TotalUnemploymentEmployer: 1200m,
        TotalIncomeTax: 6484.30m,
        TotalStampTax: 258.02m,
        TotalNet: 44257.68m,
        TotalOtherDeductions: 0m);

    public static PayrollRunTotals WorkedExampleWithDeduction(decimal otherDeductions) => WorkedExample() with
    {
        TotalNet = 44257.68m - otherDeductions,
        TotalOtherDeductions = otherDeductions,
    };

    public static decimal Debit(IReadOnlyList<GLPostingLine> lines, GLPostingKey key) =>
        lines.Where(l => l.Key == key).Sum(l => l.Debit);

    public static decimal Credit(IReadOnlyList<GLPostingLine> lines, GLPostingKey key) =>
        lines.Where(l => l.Key == key).Sum(l => l.Credit);
}

public sealed class PayrollAccrualLineBuilderTests
{
    [Fact]
    public void G1_accrual_is_one_balanced_entry_for_worked_example()
    {
        var lines = PayrollGLLines.Accrual(PayrollGLTestData.WorkedExample(), reverse: false);

        lines.Should().HaveCount(4);
        lines.Sum(l => l.Debit).Should().Be(lines.Sum(l => l.Credit));

        PayrollGLTestData.Debit(lines, GLPostingKey.LaborExpense).Should().Be(73500.00m);
        PayrollGLTestData.Credit(lines, GLPostingKey.PersonnelNetPayable).Should().Be(44257.68m);
        PayrollGLTestData.Credit(lines, GLPostingKey.TaxesPayable).Should().Be(6742.32m);
        PayrollGLTestData.Credit(lines, GLPostingKey.SgkPayable).Should().Be(22500.00m);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(99)]
    [InlineData(2026)]
    public void G2_random_multi_employee_basket_balances(int seed)
    {
        var rng = new Random(seed);
        var totals = RandomBasket(rng, employees: rng.Next(2, 50));

        var lines = PayrollGLLines.Accrual(totals, reverse: false);

        lines.Sum(l => l.Debit).Should().Be(lines.Sum(l => l.Credit));
    }

    [Fact]
    public void G6_reversal_nets_accrual_to_zero()
    {
        var totals = PayrollGLTestData.WorkedExample();
        var accrual = PayrollGLLines.Accrual(totals, reverse: false);
        var reversal = PayrollGLLines.Accrual(totals, reverse: true);

        foreach (var key in new[]
                 {
                     GLPostingKey.LaborExpense, GLPostingKey.PersonnelNetPayable,
                     GLPostingKey.TaxesPayable, GLPostingKey.SgkPayable,
                 })
        {
            var net = PayrollGLTestData.Debit(accrual, key) - PayrollGLTestData.Credit(accrual, key)
                + PayrollGLTestData.Debit(reversal, key) - PayrollGLTestData.Credit(reversal, key);
            net.Should().Be(0m);
        }
    }

    [Fact]
    public void G7_accrual_balances_when_run_has_other_deductions()
    {
        var totals = PayrollGLTestData.WorkedExampleWithDeduction(500m);

        var lines = PayrollGLLines.Accrual(totals, reverse: false);

        lines.Sum(l => l.Debit).Should().Be(lines.Sum(l => l.Credit));
        PayrollGLTestData.Debit(lines, GLPostingKey.LaborExpense).Should().Be(73500.00m);
        PayrollGLTestData.Credit(lines, GLPostingKey.PersonnelNetPayable).Should().Be(44257.68m);
        PayrollGLTestData.Credit(lines, GLPostingKey.TaxesPayable).Should().Be(6742.32m);
        PayrollGLTestData.Credit(lines, GLPostingKey.SgkPayable).Should().Be(22500.00m);
    }

    private static PayrollRunTotals RandomBasket(Random rng, int employees)
    {
        decimal gross = 0m, sgkEe = 0m, sgkEr = 0m, unEe = 0m, unEr = 0m, tax = 0m, stamp = 0m, net = 0m, other = 0m;
        for (var i = 0; i < employees; i++)
        {
            var g = Math.Round((decimal)(rng.NextDouble() * 90000 + 26000), 2);
            var se = Math.Round(g * 0.14m, 2);
            var sr = Math.Round(g * 0.205m, 2);
            var ue = Math.Round(g * 0.01m, 2);
            var ur = Math.Round(g * 0.02m, 2);
            var it = Math.Round(g * 0.12m, 2);
            var st = Math.Round(g * 0.00759m, 2);
            var od = Math.Round((decimal)(rng.NextDouble() * 2000), 2);
            var n = g - se - ue - it - st - od;
            gross += g; sgkEe += se; sgkEr += sr; unEe += ue; unEr += ur; tax += it; stamp += st; net += n; other += od;
        }
        return new PayrollRunTotals(gross, sgkEe, sgkEr, unEe, unEr, tax, stamp, net, other);
    }
}

public sealed class PayrollAccrualGLHandlerTests
{
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IPayrollRunRepository _runs = Substitute.For<IPayrollRunRepository>();
    private readonly List<GLPostingRequest> _enqueued = new();
    private readonly PayrollAccrualGLHandler _sut;

    public PayrollAccrualGLHandlerTests()
    {
        _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => _enqueued.Add(r)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _sut = new PayrollAccrualGLHandler(_outbox, _runs);
    }

    private static PayrollRun PostedRun()
    {
        var run = new PayrollRun("BORD-2026-00006", 2026, 6, Guid.NewGuid()) { Id = Guid.NewGuid() };
        run.ApplyTotals(60000m, 8400m, 12300m, 600m, 1200m, 6484.30m, 258.02m, 15742.32m, 44257.68m, 73500m, 1);
        run.Calculate();
        run.Approve(Guid.NewGuid());
        run.MarkPosted();
        return run;
    }

    [Fact]
    public async Task Enqueues_one_balanced_accrual_at_period_end()
    {
        var run = PostedRun();
        _runs.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        await _sut.Handle(PostedEvent(run), default);

        _enqueued.Should().ContainSingle();
        var req = _enqueued.Single();
        req.SourceType.Should().Be(JournalSourceType.PayrollAccrual);
        req.SourceDocumentId.Should().Be(run.Id);
        req.PostingDate.Should().Be(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc));
        req.EntryType.Should().Be(JournalEntryType.Mahsup);
        req.Lines.Sum(l => l.Debit).Should().Be(req.Lines.Sum(l => l.Credit));
        PayrollGLTestData.Debit(req.Lines, GLPostingKey.LaborExpense).Should().Be(73500.00m);
    }

    [Fact]
    public async Task Skips_when_run_not_found()
    {
        _runs.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        await _sut.Handle(PostedEvent(PostedRun()), default);

        _enqueued.Should().BeEmpty();
    }

    private static PayrollRunPostedEvent PostedEvent(PayrollRun run) =>
        new(Guid.NewGuid(), run.Id, run.RunNumber, run.PeriodYear, run.PeriodMonth, run.TotalNet, run.TotalEmployerCost, DateTime.UtcNow);
}

public sealed class PayrollAccrualGLIdempotencyTests
{
    private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
    private readonly IGLAccountRepository _accounts = Substitute.For<IGLAccountRepository>();
    private readonly IGLPostingMappingRepository _mappings = Substitute.For<IGLPostingMappingRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly List<GLAccount> _chart = new();
    private readonly GLPostingService _sut;

    private static readonly Guid RunId = Guid.NewGuid();

    public PayrollAccrualGLIdempotencyTests()
    {
        _sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));
        _accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_chart);
        _mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
        _chart.Add(new GLAccount("720", "Direkt İşçilik Gideri", AccountType.Expense, isPostable: true));
        _chart.Add(new GLAccount("335", "Personele Borçlar", AccountType.Liability, isPostable: true));
        _chart.Add(new GLAccount("360", "Ödenecek Vergi", AccountType.Liability, isPostable: true));
        _chart.Add(new GLAccount("361", "Ödenecek SGK", AccountType.Liability, isPostable: true));
        _sut = new GLPostingService(_journals, _accounts, _mappings, _sequences, _periods);
    }

    private static GLPostingRequest AccrualRequest() => new(
        JournalSourceType.PayrollAccrual,
        RunId,
        "BORD-2026-00006",
        new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        JournalEntryType.Mahsup,
        "Bordro tahakkuku BORD-2026-00006",
        PayrollGLLines.Accrual(PayrollGLTestData.WorkedExample(), reverse: false));

    [Fact]
    public async Task G3_double_post_enqueues_once()
    {
        var first = await _sut.PostAsync(AccrualRequest(), default);
        first.Should().Be(GLPostingResult.Posted);

        await _journals.Received(1).AddAsync(
            Arg.Is<JournalEntry>(j =>
                j.Status == JournalEntryStatus.Posted &&
                j.TotalDebit == 73500m &&
                j.TotalCredit == 73500m &&
                j.SourceType == JournalSourceType.PayrollAccrual &&
                j.SourceDocumentId == RunId &&
                j.Lines.Count == 4),
            Arg.Any<CancellationToken>());

        _journals.ExistsForSourceAsync(JournalSourceType.PayrollAccrual, RunId, Arg.Any<CancellationToken>())
            .Returns(true);

        var second = await _sut.PostAsync(AccrualRequest(), default);

        second.Should().Be(GLPostingResult.SkippedDuplicate);
        await _journals.Received(1).AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }
}

public sealed class PayrollPaymentGLHandlerTests
{
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly List<GLPostingRequest> _enqueued = new();

    public PayrollPaymentGLHandlerTests()
    {
        _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => _enqueued.Add(r)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task G5_three_payments_net_each_control_account_to_zero()
    {
        var totals = PayrollGLTestData.WorkedExample();
        var accrual = PayrollGLLines.Accrual(totals, reverse: false);

        var runId = Guid.NewGuid();
        var netHandler = new PayrollNetPaymentGLHandler(_outbox);
        await netHandler.Handle(
            new PayrollRunPaidEvent(Guid.NewGuid(), runId, "BORD-2026-00006", 2026, 6, totals.TotalNet, DateTime.UtcNow),
            default);

        var taxHandler = new PayPayrollTaxesHandler(_outbox);
        await taxHandler.Handle(
            new PayPayrollTaxesCommand(Guid.NewGuid(), totals.TotalIncomeTax + totals.TotalStampTax, DateTime.UtcNow, "MUH-2026-06"),
            default);

        var sgkHandler = new PayPayrollSgkHandler(_outbox);
        var sgkRemittance = totals.TotalSgkEmployee + totals.TotalUnemploymentEmployee
            + totals.TotalSgkEmployer + totals.TotalUnemploymentEmployer;
        await sgkHandler.Handle(
            new PayPayrollSgkCommand(Guid.NewGuid(), sgkRemittance, DateTime.UtcNow, "SGK-2026-06"),
            default);

        _enqueued.Should().HaveCount(3);
        foreach (var req in _enqueued)
        {
            req.Lines.Sum(l => l.Debit).Should().Be(req.Lines.Sum(l => l.Credit));
            req.EntryType.Should().Be(JournalEntryType.Tediye);
        }

        var paymentLines = _enqueued.SelectMany(r => r.Lines).ToList();
        foreach (var key in new[]
                 {
                     GLPostingKey.PersonnelNetPayable, GLPostingKey.TaxesPayable, GLPostingKey.SgkPayable,
                 })
        {
            var accrualCredit = PayrollGLTestData.Credit(accrual, key);
            var paymentDebit = PayrollGLTestData.Debit(paymentLines, key);
            (accrualCredit - paymentDebit).Should().Be(0m);
        }

        PayrollGLTestData.Credit(paymentLines, GLPostingKey.Bank).Should().Be(
            totals.TotalNet + totals.TotalIncomeTax + totals.TotalStampTax + sgkRemittance);
    }

    [Fact]
    public async Task Net_payment_uses_posted_run_id_as_idempotency_key()
    {
        var runId = Guid.NewGuid();
        var handler = new PayrollNetPaymentGLHandler(_outbox);

        await handler.Handle(
            new PayrollRunPaidEvent(Guid.NewGuid(), runId, "BORD-2026-00006", 2026, 6, 44257.68m, DateTime.UtcNow),
            default);

        var req = _enqueued.Single();
        req.SourceType.Should().Be(JournalSourceType.PayrollNetPayment);
        req.SourceDocumentId.Should().Be(runId);
        PayrollGLTestData.Debit(req.Lines, GLPostingKey.PersonnelNetPayable).Should().Be(44257.68m);
        PayrollGLTestData.Credit(req.Lines, GLPostingKey.Bank).Should().Be(44257.68m);
    }

    [Fact]
    public async Task Tax_and_sgk_commands_use_their_own_payment_doc_id()
    {
        var taxDoc = Guid.NewGuid();
        var sgkDoc = Guid.NewGuid();

        await new PayPayrollTaxesHandler(_outbox).Handle(
            new PayPayrollTaxesCommand(taxDoc, 6742.32m, DateTime.UtcNow, "MUH-2026-06"), default);
        await new PayPayrollSgkHandler(_outbox).Handle(
            new PayPayrollSgkCommand(sgkDoc, 22500m, DateTime.UtcNow, "SGK-2026-06"), default);

        _enqueued[0].SourceType.Should().Be(JournalSourceType.PayrollTaxPayment);
        _enqueued[0].SourceDocumentId.Should().Be(taxDoc);
        _enqueued[1].SourceType.Should().Be(JournalSourceType.PayrollSgkPayment);
        _enqueued[1].SourceDocumentId.Should().Be(sgkDoc);
    }
}
