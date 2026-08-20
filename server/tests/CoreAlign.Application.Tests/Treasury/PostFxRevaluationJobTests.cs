using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Treasury;

public class PostFxRevaluationJobTests
{
    private readonly IExchangeRateRepository _rates = Substitute.For<IExchangeRateRepository>();
    private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
    private readonly IFxOpenBalanceReader _balances = Substitute.For<IFxOpenBalanceReader>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly List<GLPostingRequest> _enqueued = new();
    private readonly PostFxRevaluationJob _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public PostFxRevaluationJobTests()
    {
        _tenant.PushScope(Arg.Any<Guid>()).Returns(Substitute.For<IDisposable>());
        _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => _enqueued.Add(r)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _sut = new PostFxRevaluationJob(_rates, _journals, _balances, _tenant, _outbox, NullLogger<PostFxRevaluationJob>.Instance);
    }

    private void SetRates(DateTime asOf, params (string Currency, decimal Rate)[] rates) =>
        _rates.GetLatestPerCurrencyOnOrBeforeAsync(asOf, Arg.Any<CancellationToken>())
            .Returns(rates.Select(r => new ExchangeRate { Currency = r.Currency, RateAgainstTry = r.Rate, ValidOnDate = asOf }).ToList());

    private void SetBalances(DateTime asOf, params OpenForeignBalance[] balances) =>
        _balances.GetOpenForeignBalancesAsync(asOf, Arg.Any<CancellationToken>())
            .Returns(balances.ToList());

    [Fact]
    public async Task Books_one_balanced_entry_with_stable_source_key_for_a_receivable_gain()
    {
        var asOf = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        SetRates(asOf, ("USD", 32m));
        SetBalances(asOf, new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: true, TenantId));

        var tenants = await _sut.RunAsync(asOf);

        tenants.Should().Be(1);
        _enqueued.Should().ContainSingle();
        var req = _enqueued.Single();
        req.SourceType.Should().Be(JournalSourceType.FxRevaluation);
        req.SourceDocumentId.Should().Be(FxRevaluation.SourceKey(TenantId, asOf));
        req.PostingDate.Should().Be(asOf.Date);
        // DR 120 / CR 646 for 2000 TRY (1000 USD * (32-30)).
        req.Lines.Should().HaveCount(2);
        req.Lines.Sum(l => l.Debit).Should().Be(req.Lines.Sum(l => l.Credit));
        req.Lines.Should().Contain(l => l.Key == GLPostingKey.AccountsReceivable && l.Debit == 2000m);
        req.Lines.Should().Contain(l => l.Key == GLPostingKey.FxGain && l.Credit == 2000m);
    }

    [Fact]
    public async Task Rerun_for_same_asOf_produces_the_same_idempotency_key()
    {
        var asOf = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        SetRates(asOf, ("USD", 32m));
        SetBalances(asOf, new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: true, TenantId));

        await _sut.RunAsync(asOf);
        await _sut.RunAsync(asOf);

        // Both runs enqueue with the identical stable key, so GLPostingService
        // (ExistsForSourceAsync) dedupes the second as SkippedDuplicate — no double post.
        _enqueued.Should().HaveCount(2);
        _enqueued[0].SourceDocumentId.Should().Be(_enqueued[1].SourceDocumentId);
        _enqueued[0].SourceDocumentId.Should().Be(FxRevaluation.SourceKey(TenantId, asOf));
    }

    [Fact]
    public async Task Second_month_reverses_prior_mark_and_rebooks_so_net_equals_current()
    {
        // Month 1: receivable 1000 USD booked at 30, current 32 → +2000 gain.
        var jan = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        SetRates(jan, ("USD", 32m));
        SetBalances(jan, new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: true, TenantId));
        _journals.GetMostRecentBySourceTypeBeforeAsync(JournalSourceType.FxRevaluation, jan.Date, Arg.Any<CancellationToken>())
            .Returns((JournalEntry?)null);

        await _sut.RunAsync(jan);
        var janEntry = MaterializePostedEntry(_enqueued.Single());

        // Month 2: same balance still booked at 30, current now 35 → cumulative +5000.
        _enqueued.Clear();
        var feb = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        SetRates(feb, ("USD", 35m));
        SetBalances(feb, new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: true, TenantId));
        _journals.GetPostedSourceTypeAccountNetsBeforeAsync(JournalSourceType.FxRevaluation, feb.Date, Arg.Any<CancellationToken>())
            .Returns(AccountNets(new[] { janEntry }));

        await _sut.RunAsync(feb);

        var febReq = _enqueued.Single();
        febReq.Lines.Sum(l => l.Debit).Should().Be(febReq.Lines.Sum(l => l.Credit));

        // The Feb entry reverses Jan's +2000 mark and rebooks the current full +5000,
        // so its OWN net AR delta is +3000. The ledger CUMULATIVE position across both
        // months is Jan(+2000) + Feb(+3000) = +5000 = the current mark — no double-count.
        var febNetAr = febReq.Lines.Where(l => l.Key == GLPostingKey.AccountsReceivable).Sum(l => l.Debit - l.Credit);
        var febNetGain = febReq.Lines.Where(l => l.Key == GLPostingKey.FxGain).Sum(l => l.Credit - l.Debit);
        febNetAr.Should().Be(3000m);
        febNetGain.Should().Be(3000m);

        var janNetAr = janEntry.Lines.Where(l => l.AccountCode == FxRevaluation.ArAccountCode).Sum(l => l.Debit - l.Credit);
        (janNetAr + febNetAr).Should().Be(5000m, "cumulative AR reval equals the current cumulative mark");
    }

    // A tenant whose foreign exposure has been settled produces no open balance, so iterating
    // balances alone skipped it entirely and its previous unrealized mark stayed on the books
    // forever — AR and the FX P&L permanently overstated by the last mark.
    [Fact]
    public async Task A_settled_exposure_still_reverses_the_carried_mark()
    {
        var jan = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        SetRates(jan, ("USD", 32m));
        SetBalances(jan, new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: true, TenantId));
        await _sut.RunAsync(jan);
        var janEntry = MaterializePostedEntry(_enqueued.Single());

        // February: the customer paid, so there is no open foreign balance left anywhere.
        _enqueued.Clear();
        var feb = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        SetRates(feb, ("USD", 35m));
        SetBalances(feb);
        _journals.GetTenantIdsWithPostedSourceTypeBeforeAsync(
                JournalSourceType.FxRevaluation, feb.Date, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { TenantId });
        _journals.GetPostedSourceTypeAccountNetsBeforeAsync(JournalSourceType.FxRevaluation, feb.Date, Arg.Any<CancellationToken>())
            .Returns(AccountNets(new[] { janEntry }));

        var tenants = await _sut.RunAsync(feb);

        tenants.Should().Be(1);
        var req = _enqueued.Single();
        req.Lines.Sum(l => l.Debit).Should().Be(req.Lines.Sum(l => l.Credit));

        // Pure reversal: the January mark is backed out and nothing is rebooked, so the
        // cumulative AR revaluation returns to zero.
        var febNetAr = req.Lines.Where(l => l.Key == GLPostingKey.AccountsReceivable).Sum(l => l.Debit - l.Credit);
        var janNetAr = janEntry.Lines.Where(l => l.AccountCode == FxRevaluation.ArAccountCode).Sum(l => l.Debit - l.Credit);
        febNetAr.Should().Be(-2000m);
        (janNetAr + febNetAr).Should().Be(0m, "a settled exposure carries no unrealized mark");
    }

    [Fact]
    public async Task Nothing_is_enqueued_when_there_are_neither_balances_nor_carried_marks()
    {
        var feb = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        SetRates(feb, ("USD", 35m));
        SetBalances(feb);
        _journals.GetTenantIdsWithPostedSourceTypeBeforeAsync(
                JournalSourceType.FxRevaluation, feb.Date, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var tenants = await _sut.RunAsync(feb);

        tenants.Should().Be(0);
        _enqueued.Should().BeEmpty();
    }

    // Three consecutive month-ends: the cumulative ledger position must equal the CURRENT mark.
    [Fact]
    public async Task Three_consecutive_marks_leave_the_cumulative_position_at_the_current_mark()
    {
        var entries = new List<JournalEntry>();
        var cumulativeAr = 0m;

        foreach (var (asOf, rate, expectedMark) in new[]
                 {
                     (new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), 32m, 2000m),
                     (new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), 35m, 5000m),
                     (new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), 33m, 3000m),
                 })
        {
            _enqueued.Clear();
            SetRates(asOf, ("USD", rate));
            SetBalances(asOf, new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: true, TenantId));
            // Both contracts are stubbed so the assertion is valid against the previous
            // reverse-the-last-entry implementation too: that one drifts to mark(n) + mark(n-2).
            _journals.GetMostRecentBySourceTypeBeforeAsync(JournalSourceType.FxRevaluation, asOf.Date, Arg.Any<CancellationToken>())
                .Returns(entries.Count == 0 ? null : entries[^1]);
            _journals.GetPostedSourceTypeAccountNetsBeforeAsync(JournalSourceType.FxRevaluation, asOf.Date, Arg.Any<CancellationToken>())
                .Returns(AccountNets(entries));

            await _sut.RunAsync(asOf);

            var req = _enqueued.Single();
            req.Lines.Sum(l => l.Debit).Should().Be(req.Lines.Sum(l => l.Credit));
            entries.Add(MaterializePostedEntry(req));
            cumulativeAr += req.Lines.Where(l => l.Key == GLPostingKey.AccountsReceivable).Sum(l => l.Debit - l.Credit);
            cumulativeAr.Should().Be(expectedMark, $"cumulative AR revaluation at {asOf:yyyy-MM-dd}");
        }
    }

    private static List<AccountNet> AccountNets(IEnumerable<JournalEntry> entries) =>
        entries
            .SelectMany(e => e.Lines)
            .GroupBy(l => l.AccountCode)
            .Select(g => new AccountNet(g.Key, g.Sum(l => l.Debit), g.Sum(l => l.Credit)))
            .ToList();

    // Replays the engine's translation of a balanced request into a posted JournalEntry
    // so the next run's reversal can mirror its lines (rate == 1, codes from defaults).
    private static JournalEntry MaterializePostedEntry(GLPostingRequest req)
    {
        var entry = new JournalEntry("YEV-1", req.PostingDate, req.PostingDate, req.EntryType, req.Description, req.SourceDocumentNumber);
        foreach (var l in req.Lines)
        {
            if (l.Debit <= 0m && l.Credit <= 0m) continue;
            var code = GLPostingDefaults.CodeFor(l.Key)!;
            entry.AddLine(Guid.NewGuid(), code, $"Account {code}", l.Debit, l.Credit);
        }
        entry.AssignSource(req.SourceType, req.SourceDocumentId, req.SourceDocumentNumber);
        entry.Post(Guid.Empty);
        return entry;
    }
}
