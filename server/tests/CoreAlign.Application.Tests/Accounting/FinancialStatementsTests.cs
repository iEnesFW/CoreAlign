using CoreAlign.Application.Accounting.Handlers;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAlign.Application.Tests.Accounting;

/// <summary>
/// Server-authoritative financial statements over REAL persisted journal rows
/// (EF in-memory store driven through the real <see cref="JournalEntryRepository"/>
/// join + GROUP BY/SUM). The headline guarantee: the balance sheet provably
/// balances (Assets == Liabilities + Equity + LifetimeEarnings) at ANY as-of
/// date — proven here against multiple cutoffs — because the cumulative as-of
/// sum captures every account's full position and the omitted
/// Revenue/Expense/COGS net is folded back as the retained-earnings plug.
/// </summary>
public sealed class FinancialStatementsTests : IDisposable
{
    private readonly CoreAlignDbContext _db;
    private readonly Guid _tenantId = Guid.NewGuid();

    private readonly JournalEntryRepository _journals;
    private readonly GLAccountRepository _accounts;
    private readonly CustomerLedgerRepository _customerLedger;
    private readonly VendorLedgerRepository _vendorLedger;

    private readonly Dictionary<string, GLAccount> _chart = new();

    public FinancialStatementsTests()
    {
        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(_tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(_tenantId);

        // EF InMemory: the Postgres-only xmin concurrency token and tenant FK have
        // no analog here, so we get a faithful round-trip of the real repository
        // LINQ (join + GROUP BY/SUM) over genuinely persisted journal rows without
        // fighting provider-specific DDL.
        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase($"fin-stmt-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new CoreAlignDbContext(options, tenant, Substitute.For<IPublisher>());
        _db.Database.EnsureCreated();

        _journals = new JournalEntryRepository(_db);
        _accounts = new GLAccountRepository(_db);
        _customerLedger = new CustomerLedgerRepository(_db);
        _vendorLedger = new VendorLedgerRepository(_db);
    }

    private Task SaveAsync() => _db.SaveChangesAsync();

    public void Dispose() => _db.Dispose();

    private GLAccount Account(string code, string name, AccountType type)
    {
        var account = new GLAccount(code, name, type, isPostable: true) { TenantId = _tenantId };
        _chart[code] = account;
        return account;
    }

    private async Task SeedChartAsync()
    {
        _db.GLAccounts.AddRange(
            Account("100", "Kasa", AccountType.Asset),
            Account("102", "Bankalar", AccountType.Asset),
            Account("120", "Alıcılar", AccountType.Asset),
            Account("153", "Ticari Mallar", AccountType.Asset),
            Account("320", "Satıcılar", AccountType.Liability),
            Account("391", "Hesaplanan KDV", AccountType.Liability),
            Account("500", "Sermaye", AccountType.Equity),
            Account("600", "Yurtiçi Satışlar", AccountType.Revenue),
            Account("621", "STMM", AccountType.CostOfGoodsSold),
            Account("770", "Genel Yönetim Gideri", AccountType.Expense));
        await SaveAsync();
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
    /// Books a realistic mini-ledger: capital injection, a financed inventory
    /// purchase with VAT on account, a credit sale with VAT, COGS recognition,
    /// a customer receipt, a vendor payment and an opex cash expense — spread
    /// across two calendar years so prior-year vs current-year retained earnings
    /// are both exercised.
    /// </summary>
    private async Task SeedLedgerAsync()
    {
        await SeedChartAsync();

        // 2025 (prior year)
        await PostAsync("YEV-1", new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            ("102", 100_000m, 0m), ("500", 0m, 100_000m));
        await PostAsync("YEV-2", new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            ("153", 47_200m, 0m), ("320", 0m, 47_200m));
        await PostAsync("YEV-3", new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            ("120", 23_600m, 0m), ("600", 0m, 20_000m), ("391", 0m, 3_600m));
        await PostAsync("YEV-4", new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            ("621", 12_000m, 0m), ("153", 0m, 12_000m));

        // 2026 (current year)
        await PostAsync("YEV-5", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            ("100", 23_600m, 0m), ("120", 0m, 23_600m));
        await PostAsync("YEV-6", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            ("320", 47_200m, 0m), ("102", 0m, 47_200m));
        await PostAsync("YEV-7", new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc),
            ("770", 5_000m, 0m), ("100", 0m, 5_000m));
    }

    private async Task<Guid> SeedCustomerLedgerAsync(decimal debit, decimal credit, DateTime postingDate)
    {
        var customer = new Customer("Müşteri A", code: "CUST-1") { TenantId = _tenantId };
        _db.Customers.Add(customer);
        await SaveAsync();
        if (debit > 0m)
        {
            _db.CustomerLedgerEntries.Add(new CustomerLedgerEntry(
                customer.Id, postingDate, postingDate, LedgerEntryType.Debit, debit, "TRY", 1m,
                LedgerSourceType.Invoice, null, null, null)
            { TenantId = _tenantId });
        }
        if (credit > 0m)
        {
            _db.CustomerLedgerEntries.Add(new CustomerLedgerEntry(
                customer.Id, postingDate, postingDate, LedgerEntryType.Credit, credit, "TRY", 1m,
                LedgerSourceType.Payment, null, null, null)
            { TenantId = _tenantId });
        }
        await SaveAsync();
        return customer.Id;
    }

    private GetBalanceSheetHandler BalanceSheetHandler() => new(_journals, _accounts);
    private GetIncomeStatementHandler IncomeStatementHandler() => new(_journals, _accounts);

    private GetSubledgerReconciliationHandler ReconciliationHandler() =>
        new(_journals, _customerLedger, _vendorLedger, _accounts);

    public static IEnumerable<object[]> AsOfDates() => new[]
    {
        new object[] { new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
        new object[] { new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc) },
        new object[] { new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc) },
    };

    [Theory]
    [MemberData(nameof(AsOfDates))]
    public async Task Balance_sheet_balances_at_any_as_of(DateTime asOf)
    {
        await SeedLedgerAsync();

        var bs = await BalanceSheetHandler().Handle(new GetBalanceSheetQuery(asOf), default);

        bs.IsBalanced.Should().BeTrue($"Assets {bs.Assets.Total} must equal L+E+earnings {bs.TotalLiabilitiesAndEquity} at {asOf:d}");
        Math.Abs(bs.Variance).Should().BeLessThan(0.01m);
        bs.Assets.Total.Should().BeApproximately(bs.TotalLiabilitiesAndEquity, 0.01m);
    }

    [Fact]
    public async Task Balance_sheet_excludes_pnl_accounts_and_folds_lifetime_earnings()
    {
        await SeedLedgerAsync();
        var asOf = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        var bs = await BalanceSheetHandler().Handle(new GetBalanceSheetQuery(asOf), default);

        // Revenue / COGS / Opex never appear on the sheet itself.
        bs.Assets.Lines.Should().NotContain(l => l.AccountCode == "600" || l.AccountCode == "621" || l.AccountCode == "770");
        bs.Liabilities.Lines.Should().NotContain(l => l.AccountCode == "600");
        bs.Equity.Lines.Should().NotContain(l => l.AccountCode == "600");

        // Lifetime earnings = Revenue(20000) − COGS(12000) − Opex(5000) = 3000,
        // split into prior-year (2025: 20000−12000 = 8000) and current-year
        // (2026: −5000 opex) = -5000.
        bs.RetainedPriorEarnings.Should().Be(8_000m);
        bs.CurrentYearEarnings.Should().Be(-5_000m);
        (bs.CurrentYearEarnings + bs.RetainedPriorEarnings).Should().Be(3_000m);
    }

    [Fact]
    public async Task Manual_journal_to_AR_without_subledger_surfaces_as_variance()
    {
        // GL 120 carries 23600 (YEV-3 sale, not yet receipted on the GL in this
        // window) but no CustomerLedgerEntry exists → exactly the drift the
        // reconciliation must catch.
        await SeedLedgerAsync();
        // As-of just before YEV-5 so AR 120 is still open on the GL at 23600.
        var asOf = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var rec = await ReconciliationHandler().Handle(new GetSubledgerReconciliationQuery(asOf), default);

        var ar = rec.Lines.Single(l => l.ControlCode == "120");
        ar.GlBalance.Should().Be(23_600m);
        ar.SubledgerBalance.Should().Be(0m);
        ar.Variance.Should().Be(23_600m);
        ar.IsReconciled.Should().BeFalse();
        rec.AllReconciled.Should().BeFalse();

        // Cash line is informational GL-only (no cash subledger module).
        var cash = rec.Lines.Single(l => l.ControlCode == "100+102");
        cash.Subledger.Should().Contain("GL-only");
    }

    [Fact]
    public async Task Reconciliation_matches_GL_to_subledger_when_in_sync()
    {
        // AR 120 open at 23600 on the GL as-of 2025-12-31, matched by a customer
        // ledger debit of 23600 → reconciled. Vendor 320 open at 47200 on the GL,
        // matched by a vendor credit of 47200 (handled via SeedLedger vendor side).
        await SeedLedgerAsync();
        var postingDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        await SeedCustomerLedgerAsync(debit: 23_600m, credit: 0m, postingDate);

        var vendor = new Vendor("Tedarikçi A", code: "VEND-1") { TenantId = _tenantId };
        _db.Vendors.Add(vendor);
        await SaveAsync();
        _db.VendorLedgerEntries.Add(new VendorLedgerEntry(
            vendor.Id, postingDate, postingDate, LedgerEntryType.Credit, 47_200m, "TRY", 1m,
            LedgerSourceType.Invoice, null, null, null)
        { TenantId = _tenantId });
        await SaveAsync();

        var asOf = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var rec = await ReconciliationHandler().Handle(new GetSubledgerReconciliationQuery(asOf), default);

        var ar = rec.Lines.Single(l => l.ControlCode == "120");
        ar.GlBalance.Should().Be(23_600m);
        ar.SubledgerBalance.Should().Be(23_600m);
        ar.IsReconciled.Should().BeTrue();

        var ap = rec.Lines.Single(l => l.ControlCode == "320");
        ap.GlBalance.Should().Be(47_200m);
        ap.SubledgerBalance.Should().Be(47_200m);
        ap.IsReconciled.Should().BeTrue();
    }

    [Fact]
    public async Task Income_statement_is_movement_over_the_range()
    {
        await SeedLedgerAsync();

        // Whole-history window: revenue 20000, cogs 12000, opex 5000.
        var pnl = await IncomeStatementHandler().Handle(
            new GetIncomeStatementQuery(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
            default);

        pnl.Revenue.Total.Should().Be(20_000m);
        pnl.Cogs.Total.Should().Be(12_000m);
        pnl.Opex.Total.Should().Be(5_000m);
        pnl.GrossProfit.Should().Be(8_000m);
        pnl.NetIncome.Should().Be(3_000m);
    }

    [Fact]
    public async Task Income_statement_range_excludes_out_of_window_movement()
    {
        await SeedLedgerAsync();

        // 2026 only: no revenue/cogs posted in 2026, just the 5000 opex.
        var pnl = await IncomeStatementHandler().Handle(
            new GetIncomeStatementQuery(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
            default);

        pnl.Revenue.Total.Should().Be(0m);
        pnl.Cogs.Total.Should().Be(0m);
        pnl.Opex.Total.Should().Be(5_000m);
        pnl.NetIncome.Should().Be(-5_000m);
    }

    [Fact]
    public async Task As_of_cumulative_balance_carries_opening_position_forward()
    {
        await SeedLedgerAsync();

        // Bank 102 after capital(+100000) and vendor payment(−47200) = 52800,
        // a true cumulative position that a period-bounded 2026 trial balance
        // would understate (it would show only the −47200 movement).
        var rows = await _journals.GetAccountBalancesAsOfAsync(
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc), default);

        var bank = rows.Single(r => r.AccountCode == "102");
        (bank.Debit - bank.Credit).Should().Be(52_800m);
    }
}
