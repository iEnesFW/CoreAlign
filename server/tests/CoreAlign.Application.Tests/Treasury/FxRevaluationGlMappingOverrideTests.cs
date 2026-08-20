using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Treasury;

/// <summary>
/// Regression for the net-delta FX reversal under a tenant GL-mapping override.
/// When a tenant remaps FxGain/FxLoss/AR/AP to non-default codes via
/// <see cref="GLPostingMapping"/>, the prior FX entry's lines carry the OVERRIDDEN
/// codes. The reversal must mirror those exact accounts (never re-resolve the role,
/// never drop a leg) so the second month's net-delta entry still balances and the
/// outbox posting succeeds instead of dead-lettering on
/// <c>JournalEntryNotBalancedException</c>. Each month's request is driven through
/// the REAL <see cref="GLPostingService"/> over the in-memory store so the prior
/// mark is materialized at its real (overridden) accounts.
/// </summary>
public sealed class FxRevaluationGlMappingOverrideTests : IDisposable
{
    private const string GainOverride = "647";
    private const string LossOverride = "657";
    private const string ArOverride = "121";
    private const string ApOverride = "321";

    private readonly CoreAlignDbContext _db;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly ITenantContext _tenant;

    private readonly JournalEntryRepository _journals;
    private readonly GLPostingService _gl;

    private readonly IExchangeRateRepository _rates = Substitute.For<IExchangeRateRepository>();
    private readonly IJournalEntryRepository _jobJournals = Substitute.For<IJournalEntryRepository>();
    private readonly IFxOpenBalanceReader _balances = Substitute.For<IFxOpenBalanceReader>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly List<GLPostingRequest> _enqueued = new();
    private readonly PostFxRevaluationJob _job;

    public FxRevaluationGlMappingOverrideTests()
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(_tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(_tenantId);
        tenant.PushScope(Arg.Any<Guid>()).Returns(Substitute.For<IDisposable>());
        _tenant = tenant;

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase($"fx-map-override-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new CoreAlignDbContext(options, tenant, Substitute.For<IPublisher>());
        _db.Database.EnsureCreated();

        _journals = new JournalEntryRepository(_db);
        _gl = new GLPostingService(
            _journals,
            new GLAccountRepository(_db),
            new GLPostingMappingRepository(_db),
            new DocumentSequenceRepository(_db),
            new AccountingPeriodRepository(_db));

        _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => _enqueued.Add(r)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        // The job reads the prior mark through a substitute (we feed it the entry the
        // real GLPostingService persisted) and enqueues; the real repository below is
        // what actually posts + persists each month's request.
        _job = new PostFxRevaluationJob(_rates, _jobJournals, _balances, _tenant, _outbox, NullLogger<PostFxRevaluationJob>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private void Account(string code, AccountType type) =>
        _db.GLAccounts.Add(new GLAccount(code, $"Account {code}", type, isPostable: true) { TenantId = _tenantId });

    private async Task SeedChartAndOverridesAsync()
    {
        // Both the default and the overridden codes exist and are postable so the
        // resolver can target either; the FX reval is wired to the overridden ones.
        Account("120", AccountType.Asset);
        Account("320", AccountType.Liability);
        Account("646", AccountType.Revenue);
        Account("656", AccountType.Expense);
        Account(ArOverride, AccountType.Asset);
        Account(ApOverride, AccountType.Liability);
        Account(GainOverride, AccountType.Revenue);
        Account(LossOverride, AccountType.Expense);

        _db.GLPostingMappings.AddRange(
            new GLPostingMapping(GLPostingKey.AccountsReceivable, ArOverride) { TenantId = _tenantId },
            new GLPostingMapping(GLPostingKey.AccountsPayable, ApOverride) { TenantId = _tenantId },
            new GLPostingMapping(GLPostingKey.FxGain, GainOverride) { TenantId = _tenantId },
            new GLPostingMapping(GLPostingKey.FxLoss, LossOverride) { TenantId = _tenantId });

        _db.DocumentSequences.Add(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5) { TenantId = _tenantId });
        await _db.SaveChangesAsync();
    }

    private void SetMonth(DateTime asOf, decimal rate, OpenForeignBalance balance, JournalEntry? prior)
    {
        _rates.GetLatestPerCurrencyOnOrBeforeAsync(asOf, Arg.Any<CancellationToken>())
            .Returns(new List<ExchangeRate> { new() { Currency = "USD", RateAgainstTry = rate, ValidOnDate = asOf } });
        _balances.GetOpenForeignBalancesAsync(asOf, Arg.Any<CancellationToken>())
            .Returns(new List<OpenForeignBalance> { balance });
        _jobJournals.GetPostedSourceTypeAccountNetsBeforeAsync(JournalSourceType.FxRevaluation, asOf.Date, Arg.Any<CancellationToken>())
            .Returns(prior is null
                ? new List<AccountNet>()
                : prior.Lines
                    .GroupBy(l => l.AccountCode)
                    .Select(g => new AccountNet(g.Key, g.Sum(l => l.Debit), g.Sum(l => l.Credit)))
                    .ToList());
    }

    // Runs the job for one month, then posts the single enqueued request through the
    // REAL GLPostingService — so the resulting entry is materialized at the tenant's
    // OVERRIDDEN accounts, exactly as production would persist the prior mark.
    private async Task<(GLPostingResult Result, JournalEntry Posted)> RunAndPostAsync(DateTime asOf)
    {
        _enqueued.Clear();
        await _job.RunAsync(asOf);
        var req = _enqueued.Single();
        var result = await _gl.PostAsync(req);
        await _db.SaveChangesAsync();
        var posted = await _journals.GetActiveBySourceAsync(req.SourceType, req.SourceDocumentId)
            ?? throw new InvalidOperationException("Expected the FX entry to be persisted.");
        return (result, posted);
    }

    [Fact]
    public async Task Two_consecutive_months_post_balanced_under_a_gl_mapping_override()
    {
        await SeedChartAndOverridesAsync();
        var usd = new OpenForeignBalance("USD", 1000m, 30m, IsReceivable: true, _tenantId);

        // Month 1: current 32 → +2000 gain, booked to the OVERRIDDEN AR/gain accounts.
        var jan = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        SetMonth(jan, 32m, usd, prior: null);
        var (janResult, janEntry) = await RunAndPostAsync(jan);

        janResult.Should().Be(GLPostingResult.Posted);
        janEntry.Status.Should().Be(JournalEntryStatus.Posted);
        janEntry.TotalDebit.Should().Be(janEntry.TotalCredit);
        janEntry.Lines.Should().Contain(l => l.AccountCode == ArOverride && l.Debit == 2000m);
        janEntry.Lines.Should().Contain(l => l.AccountCode == GainOverride && l.Credit == 2000m);

        // Month 2: current 35 → cumulative +5000. The job reverses Jan's overridden
        // mark and rebooks the current one. Before the fix the reversal re-resolved
        // each prior line's role by its (overridden) code, found none, dropped the
        // legs and the entry could not balance → JournalEntryNotBalancedException.
        var feb = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        SetMonth(feb, 35m, usd, prior: janEntry);

        // The fix guarantees no JournalEntryNotBalancedException is thrown here.
        var (febResult, febEntry) = await RunAndPostAsync(feb);

        febResult.Should().Be(GLPostingResult.Posted);
        febEntry.Status.Should().Be(JournalEntryStatus.Posted);
        febEntry.TotalDebit.Should().Be(febEntry.TotalCredit, "the net-delta reversal + rebook must balance");

        // No leg was dropped: the reversal contra of Jan's two lines plus the two new
        // mark lines net on the overridden accounts to +4 distinct legs collapsed by
        // account into the current cumulative position.
        var arNet = febEntry.Lines.Where(l => l.AccountCode == ArOverride).Sum(l => l.Debit - l.Credit);
        var gainNet = febEntry.Lines.Where(l => l.AccountCode == GainOverride).Sum(l => l.Credit - l.Debit);
        arNet.Should().Be(3000m, "Feb books +5000 and reverses Jan's +2000");
        gainNet.Should().Be(3000m);

        // Cumulative across both months equals the current mark — no double count, no strand.
        var janArNet = janEntry.Lines.Where(l => l.AccountCode == ArOverride).Sum(l => l.Debit - l.Credit);
        (janArNet + arNet).Should().Be(5000m);
    }
}
