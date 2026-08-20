using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.Handlers;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAlign.Application.Tests.Accounting;

/// <summary>
/// Statutory TDHP year-end close (Kapanış) + opening (Açılış) over REAL persisted
/// journal rows. Proves the exact GL legs of the numeric example, idempotency,
/// the close→open→retained-earnings roll, the reversal-window guard, and that the
/// balance sheet stays balanced across the close boundary.
/// </summary>
public sealed class YearEndCloseTests : IDisposable
{
    private readonly CoreAlignDbContext _db;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly ITenantContext _tenant;

    private readonly JournalEntryRepository _journals;
    private readonly GLAccountRepository _accounts;
    private readonly DocumentSequenceRepository _sequences;

    private readonly Dictionary<string, GLAccount> _chart = new();

    public YearEndCloseTests()
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(_tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(_tenantId);
        _tenant = tenant;

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase($"yearend-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new CoreAlignDbContext(options, tenant, Substitute.For<MediatR.IPublisher>());
        _db.Database.EnsureCreated();

        _journals = new JournalEntryRepository(_db);
        _accounts = new GLAccountRepository(_db);
        _sequences = new DocumentSequenceRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private Task SaveAsync() => _db.SaveChangesAsync();
    private UnitOfWork Uow() => new(_db);

    private static IAccountingPeriodRepository NoPeriods()
    {
        var periods = Substitute.For<IAccountingPeriodRepository>();
        periods.ListAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(System.Array.Empty<AccountingPeriod>());
        return periods;
    }

    private CloseFiscalYearHandler Close(IAccountingPeriodRepository? periods = null) =>
        new(_journals, _accounts, periods ?? NoPeriods(), _sequences, _tenant, Uow());

    private OpenFiscalYearHandler Open() =>
        new(_journals, _accounts, _sequences, _tenant, Uow());

    private ReverseFiscalYearCloseHandler ReverseClose() =>
        new(_journals, _sequences, _tenant, Uow());

    private GetBalanceSheetHandler BalanceSheet() => new(_journals, _accounts, _tenant);

    private GLAccount Account(string code, string name, AccountType type)
    {
        var account = new GLAccount(code, name, type, isPostable: true) { TenantId = _tenantId };
        _chart[code] = account;
        return account;
    }

    private async Task SeedAsync()
    {
        _db.GLAccounts.AddRange(
            Account("100", "Kasa", AccountType.Asset),
            Account("120", "Alıcılar", AccountType.Asset),
            Account("320", "Satıcılar", AccountType.Liability),
            Account("500", "Sermaye", AccountType.Equity),
            Account("570", "Geçmiş Yıllar Kârları", AccountType.Equity),
            Account("580", "Geçmiş Yıllar Zararları (-)", AccountType.Equity),
            Account("590", "Dönem Net Kârı", AccountType.Equity),
            Account("591", "Dönem Net Zararı (-)", AccountType.Equity),
            Account("600", "Yurtiçi Satışlar", AccountType.Revenue),
            Account("610", "Satıştan İadeler (-)", AccountType.Revenue),
            Account("621", "STMM", AccountType.CostOfGoodsSold),
            Account("632", "Genel Yönetim Gideri", AccountType.Expense),
            Account("690", "Dönem Kârı veya Zararı", AccountType.Revenue));
        _db.DocumentSequences.Add(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5) { TenantId = _tenantId });
        await SaveAsync();
    }

    // The sweep zeroes each P&L leaf at its from-inception balance, so closing 2027 while 2026
    // is still open rolls 2026's result into 2027: 590 reports two years of profit and 2026 never
    // shows its own. The date-ranged income statement would then disagree with the closing entry.
    [Fact]
    public async Task Closing_a_year_while_an_earlier_one_is_still_open_is_refused()
    {
        await SeedAsync();
        await PostAsync("YEV-1", new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            ("120", 1000m, 0m), ("600", 0m, 1000m));
        await PostAsync("YEV-2", new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            ("120", 400m, 0m), ("600", 0m, 400m));

        var act = () => Close().Handle(new CloseFiscalYearCommand(2027, Guid.Empty), default);

        await act.Should().ThrowAsync<EarlierFiscalYearNotClosedException>();
    }

    [Fact]
    public async Task Closing_the_years_oldest_first_succeeds_and_each_year_reports_its_own_result()
    {
        await SeedAsync();
        await PostAsync("YEV-1", new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            ("120", 1000m, 0m), ("600", 0m, 1000m));
        await PostAsync("YEV-2", new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            ("120", 400m, 0m), ("600", 0m, 400m));

        var first = await Close().Handle(new CloseFiscalYearCommand(2026, Guid.Empty), default);
        var second = await Close().Handle(new CloseFiscalYearCommand(2027, Guid.Empty), default);

        first.NetResult.Should().Be(1000m);
        second.NetResult.Should().Be(400m, "2027 reports only its own result");
    }

    // A tenant in its first year has nothing before the start, so the guard must not fire.
    [Fact]
    public async Task A_first_year_close_is_not_blocked_by_the_guard()
    {
        await SeedAsync();
        await PostAsync("YEV-1", new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            ("120", 1000m, 0m), ("600", 0m, 1000m));

        var result = await Close().Handle(new CloseFiscalYearCommand(2026, Guid.Empty), default);

        result.NetResult.Should().Be(1000m);
    }

    private async Task PostAsync(string number, DateTime postingDate, params (string Code, decimal Debit, decimal Credit)[] lines)
    {
        var entry = new JournalEntry(number, postingDate, postingDate, JournalEntryType.Mahsup) { TenantId = _tenantId };
        foreach (var (code, debit, credit) in lines)
        {
            var a = _chart[code];
            entry.AddLine(a.Id, a.Code, a.Name, debit, credit);
        }
        entry.Post(Guid.Empty);
        await _journals.AddAsync(entry);
        await SaveAsync();
    }

    /// <summary>
    /// The exact numeric example: capital + cash, revenue 600 = 100000 (credit),
    /// COGS 621 = 60000 (debit), opex 632 = 20000 (debit). After the books:
    /// Cash 100 = 130000 dr, AP 320 = 50000 cr, Capital 500 = 60000 cr.
    /// </summary>
    private async Task SeedExampleLedgerAsync()
    {
        await SeedAsync();
        var d = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        await PostAsync("YEV-1", d, ("100", 60_000m, 0m), ("500", 0m, 60_000m));   // capital
        await PostAsync("YEV-2", d, ("100", 100_000m, 0m), ("600", 0m, 100_000m)); // cash sale, revenue 100000
        await PostAsync("YEV-3", d, ("621", 60_000m, 0m), ("320", 0m, 60_000m));   // COGS on account
        await PostAsync("YEV-4", d, ("632", 20_000m, 0m), ("320", 0m, 20_000m));   // opex on account
        // Pay down AP so 320 nets to 50000 cr and cash to 130000 dr.
        await PostAsync("YEV-5", d, ("320", 30_000m, 0m), ("100", 0m, 30_000m));
    }

    private decimal Natural(IReadOnlyList<AccountBalanceRow> rows, string code)
    {
        var r = rows.FirstOrDefault(x => x.AccountCode == code);
        if (r is null) return 0m;
        var account = _chart[code];
        return account.NormalSide == NormalSide.Debit ? r.Debit - r.Credit : r.Credit - r.Debit;
    }

    [Fact]
    public async Task Close_sweeps_6xx_to_690_and_transfers_profit_to_590()
    {
        await SeedExampleLedgerAsync();

        var result = await Close().Handle(new CloseFiscalYearCommand(2026), default);

        result.AlreadyExisted.Should().BeFalse();
        result.Entry.Type.Should().Be(JournalEntryType.Kapanis);
        result.Entry.Status.Should().Be(JournalEntryStatus.Posted);
        result.Entry.TotalDebit.Should().Be(result.Entry.TotalCredit);
        result.NetResult.Should().Be(20_000m); // 100000 rev − 80000 (cogs+opex)

        var dec31 = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var after = await _journals.GetAccountBalancesAsOfAsync(dec31, default);

        // 6xx and 690 are flat after the close.
        Natural(after, "600").Should().Be(0m);
        Natural(after, "621").Should().Be(0m);
        Natural(after, "632").Should().Be(0m);
        Natural(after, "690").Should().Be(0m);
        // Net profit sits in equity 590.
        Natural(after, "590").Should().Be(20_000m);
        // Balance-sheet accounts are untouched by the close.
        Natural(after, "100").Should().Be(130_000m);
        Natural(after, "320").Should().Be(50_000m);
        Natural(after, "500").Should().Be(60_000m);
    }

    [Fact]
    public async Task Close_books_a_loss_to_591()
    {
        await SeedAsync();
        var d = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        await PostAsync("YEV-1", d, ("100", 10_000m, 0m), ("500", 0m, 10_000m));
        await PostAsync("YEV-2", d, ("100", 30_000m, 0m), ("600", 0m, 30_000m)); // revenue 30000
        await PostAsync("YEV-3", d, ("632", 50_000m, 0m), ("100", 0m, 50_000m)); // opex 50000 → loss 20000

        var result = await Close().Handle(new CloseFiscalYearCommand(2026), default);

        result.NetResult.Should().Be(-20_000m);
        var dec31 = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var after = await _journals.GetAccountBalancesAsOfAsync(dec31, default);
        Natural(after, "690").Should().Be(0m);
        Natural(after, "600").Should().Be(0m);
        Natural(after, "632").Should().Be(0m);
        // 591 carries a debit balance (contra-equity) of the loss.
        Natural(after, "591").Should().Be(-20_000m);
    }

    [Fact]
    public async Task Close_flattens_contra_revenue_carrying_a_debit_balance()
    {
        await SeedAsync();
        var d = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        await PostAsync("YEV-1", d, ("100", 100_000m, 0m), ("600", 0m, 100_000m)); // revenue 100000
        await PostAsync("YEV-2", d, ("610", 15_000m, 0m), ("100", 0m, 15_000m));   // contra-revenue (debit)

        await Close().Handle(new CloseFiscalYearCommand(2026), default);

        var dec31 = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var after = await _journals.GetAccountBalancesAsOfAsync(dec31, default);
        Natural(after, "600").Should().Be(0m);
        Natural(after, "610").Should().Be(0m);
        // Net revenue 85000 → profit in 590.
        Natural(after, "590").Should().Be(85_000m);
    }

    [Fact]
    public async Task Close_sweeps_691_tax_and_692_so_post_closing_balance_sheet_balances()
    {
        await SeedAsync();
        _db.GLAccounts.AddRange(
            Account("370", "Dönem Kârı Vergi ve Diğer Yasal Yük. Karşılıkları", AccountType.Liability),
            Account("691", "Dönem Kârı Vergi Karşılığı (-)", AccountType.Expense),
            Account("692", "Dönem Net Kârı veya Zararı", AccountType.Revenue));
        await SaveAsync();

        var d = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        await PostAsync("YEV-1", d, ("100", 60_000m, 0m), ("500", 0m, 60_000m));    // capital
        await PostAsync("YEV-2", d, ("100", 100_000m, 0m), ("600", 0m, 100_000m));  // revenue 100000
        await PostAsync("YEV-3", d, ("621", 60_000m, 0m), ("320", 0m, 60_000m));    // COGS 60000
        await PostAsync("YEV-4", d, ("632", 20_000m, 0m), ("320", 0m, 20_000m));    // opex 20000 → pre-tax 20000
        await PostAsync("YEV-5", d, ("320", 30_000m, 0m), ("100", 0m, 30_000m));    // pay AP → 320=50000, cash=130000
        await PostAsync("YEV-6", d, ("691", 4_000m, 0m), ("370", 0m, 4_000m));      // tax provision 4000

        var result = await Close().Handle(new CloseFiscalYearCommand(2026), default);

        // Net result is AFTER tax: 100000 − 60000 − 20000 − 4000 = 16000.
        result.NetResult.Should().Be(16_000m);
        result.Entry.TotalDebit.Should().Be(result.Entry.TotalCredit);

        var dec31 = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var after = await _journals.GetAccountBalancesAsOfAsync(dec31, default);
        // Tax provision (691) and 692 are swept flat, not stranded on the books.
        Natural(after, "691").Should().Be(0m);
        Natural(after, "692").Should().Be(0m);
        Natural(after, "690").Should().Be(0m);
        Natural(after, "590").Should().Be(16_000m);

        // The post-closing balance sheet balances WITH the tax liability standing:
        // assets 130000 = AP 50000 + tax 4000 + capital 60000 + net 16000.
        var bs = await BalanceSheet().Handle(new GetBalanceSheetQuery(dec31), default);
        bs.IsBalanced.Should().BeTrue();
        bs.Liabilities.Lines.Should().Contain(l => l.AccountCode == "370" && Math.Abs(l.Amount - 4_000m) < 0.01m);
        bs.Equity.Lines.Should().Contain(l => l.AccountCode == "590" && Math.Abs(l.Amount - 16_000m) < 0.01m);
    }

    [Fact]
    public async Task Close_is_idempotent_per_year()
    {
        await SeedExampleLedgerAsync();

        var first = await Close().Handle(new CloseFiscalYearCommand(2026), default);
        var second = await Close().Handle(new CloseFiscalYearCommand(2026), default);

        second.AlreadyExisted.Should().BeTrue();
        second.Entry.Id.Should().Be(first.Entry.Id);

        var kapanis = await _journals.SearchAsync(null, JournalEntryType.Kapanis, null, null, null, 1, 50, default);
        kapanis.Total.Should().Be(1);
    }

    [Fact]
    public async Task Open_rolls_590_to_570_and_reopens_balance_sheet()
    {
        await SeedExampleLedgerAsync();
        await Close().Handle(new CloseFiscalYearCommand(2026), default);

        var result = await Open().Handle(new OpenFiscalYearCommand(2026), default);

        result.AlreadyExisted.Should().BeFalse();
        result.Entry.Type.Should().Be(JournalEntryType.Acilis);
        result.Entry.TotalDebit.Should().Be(result.Entry.TotalCredit);

        // The açılış lines: DR 100 130000, CR 320 50000, CR 500 60000, CR 570 20000.
        var lines = result.Entry.Lines;
        lines.Single(l => l.AccountCode == "100").Debit.Should().Be(130_000m);
        lines.Single(l => l.AccountCode == "320").Credit.Should().Be(50_000m);
        lines.Single(l => l.AccountCode == "500").Credit.Should().Be(60_000m);
        lines.Single(l => l.AccountCode == "570").Credit.Should().Be(20_000m);
        // 590/591 are NOT carried forward.
        lines.Should().NotContain(l => l.AccountCode == "590" || l.AccountCode == "591");
    }

    [Fact]
    public async Task Open_requires_the_close_to_exist_first()
    {
        await SeedExampleLedgerAsync();

        Func<Task> act = () => Open().Handle(new OpenFiscalYearCommand(2026), default);

        await act.Should().ThrowAsync<FiscalYearCloseNotFoundException>();
    }

    [Fact]
    public async Task Balance_sheet_balances_before_and_after_close_and_open()
    {
        await SeedExampleLedgerAsync();

        // BEFORE close: open-year fold captures the whole 2026 P&L.
        var preClose = await BalanceSheet().Handle(
            new GetBalanceSheetQuery(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)), default);
        preClose.IsBalanced.Should().BeTrue();
        preClose.CurrentYearEarnings.Should().Be(20_000m);

        await Close().Handle(new CloseFiscalYearCommand(2026), default);

        // AT close date: 590 now holds the result; fold drops to 0 (close exists).
        var atClose = await BalanceSheet().Handle(
            new GetBalanceSheetQuery(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)), default);
        atClose.IsBalanced.Should().BeTrue();
        atClose.CurrentYearEarnings.Should().Be(0m);
        atClose.Equity.Lines.Should().Contain(l => l.AccountCode == "590" && Math.Abs(l.Amount - 20_000m) < 0.01m);

        await Open().Handle(new OpenFiscalYearCommand(2026), default);

        // NEXT year: 570 holds the rolled retained earnings, 590 reopened empty,
        // assets/liabilities re-opened — and NO double count.
        var nextYear = await BalanceSheet().Handle(
            new GetBalanceSheetQuery(new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc)), default);
        nextYear.IsBalanced.Should().BeTrue();
        nextYear.Assets.Lines.Single(l => l.AccountCode == "100").Amount.Should().Be(130_000m);
        nextYear.Equity.Lines.Should().Contain(l => l.AccountCode == "570" && Math.Abs(l.Amount - 20_000m) < 0.01m);
        nextYear.Equity.Lines.Should().NotContain(l => l.AccountCode == "590");
    }

    [Fact]
    public async Task Close_can_be_reversed_while_unconsumed_then_re_closed()
    {
        await SeedExampleLedgerAsync();
        await Close().Handle(new CloseFiscalYearCommand(2026), default);

        var reversal = await ReverseClose().Handle(new ReverseFiscalYearCloseCommand(2026), default);
        reversal.Entry.Type.Should().Be(JournalEntryType.Kapanis);

        // After reversal, 6xx are re-inflated and 590 is back to zero.
        var dec31 = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var after = await _journals.GetAccountBalancesAsOfAsync(dec31, default);
        Natural(after, "600").Should().Be(100_000m);
        Natural(after, "590").Should().Be(0m);

        // The reversed close is treated as absent, so a corrected close re-posts.
        var reClose = await Close().Handle(new CloseFiscalYearCommand(2026), default);
        reClose.AlreadyExisted.Should().BeFalse();
        reClose.Entry.Status.Should().Be(JournalEntryStatus.Posted);
        Natural(await _journals.GetAccountBalancesAsOfAsync(dec31, default), "590").Should().Be(20_000m);
    }

    [Fact]
    public async Task Close_cannot_be_reversed_once_consumed_by_the_opening()
    {
        await SeedExampleLedgerAsync();
        await Close().Handle(new CloseFiscalYearCommand(2026), default);
        await Open().Handle(new OpenFiscalYearCommand(2026), default);

        Func<Task> act = () => ReverseClose().Handle(new ReverseFiscalYearCloseCommand(2026), default);

        await act.Should().ThrowAsync<FiscalYearAlreadyOpenedException>();
    }

    [Fact]
    public async Task Close_blocked_when_a_monthly_period_is_still_open()
    {
        await SeedExampleLedgerAsync();
        var periods = Substitute.For<IAccountingPeriodRepository>();
        var open = new AccountingPeriod(2026, 12);
        periods.ListAsync(2026, Arg.Any<CancellationToken>()).Returns(new[] { open });

        Func<Task> act = () => Close(periods).Handle(new CloseFiscalYearCommand(2026), default);

        await act.Should().ThrowAsync<YearNotReadyForCloseException>();
    }
}
