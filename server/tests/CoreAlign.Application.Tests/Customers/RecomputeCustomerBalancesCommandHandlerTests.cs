using CoreAlign.Application.Customers.Maintenance;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Customers;

public sealed class RecomputeCustomerBalancesCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly RecomputeCustomerBalancesCommandHandler _sut;

    private readonly Guid _customerId = Guid.NewGuid();

    public RecomputeCustomerBalancesCommandHandlerTests()
    {
        _sut = new RecomputeCustomerBalancesCommandHandler(_customers, _ledger, _reports, _journals, _uow, _tenant);
        _reports.GetOpenInvoicesAcrossCustomersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<OpenInvoiceRow>());
        _journals.GetAccountBalancesAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<AccountBalanceRow>());
    }

    private Customer StaleCustomer(decimal storedBalance, decimal storedOverdue)
    {
        var customer = new Customer("Acme") { Id = _customerId, TenantId = Guid.NewGuid() };
        customer.RecalculateBalance(storedBalance, storedOverdue);
        _customers.SearchAsync(null, null, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Customer>)new[] { customer }, 1));
        return customer;
    }

    [Fact]
    public async Task DryRun_reports_drift_without_writing()
    {
        var customer = StaleCustomer(storedBalance: 100m, storedOverdue: 0m);
        _ledger.GetCurrentBalanceAsync(_customerId, Arg.Any<CancellationToken>()).Returns(250m);

        var result = await _sut.Handle(new RecomputeCustomerBalancesCommand(DryRun: true), default);

        result.Drifted.Should().Be(1);
        result.Recomputed.Should().Be(0);
        result.Drifts.Single().LedgerBalance.Should().Be(250m);
        result.Drifts.Single().StoredBalance.Should().Be(100m);
        customer.CurrentBalance.Should().Be(100m);
        _customers.DidNotReceive().Update(Arg.Any<Customer>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Heal_recomputes_current_balance_from_ledger_and_persists()
    {
        var customer = StaleCustomer(storedBalance: 100m, storedOverdue: 0m);
        _ledger.GetCurrentBalanceAsync(_customerId, Arg.Any<CancellationToken>()).Returns(250m);

        var result = await _sut.Handle(new RecomputeCustomerBalancesCommand(DryRun: false), default);

        result.Recomputed.Should().Be(1);
        customer.CurrentBalance.Should().Be(250m);
        _customers.Received(1).Update(customer);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Overdue_is_recomputed_from_past_due_open_invoices_only()
    {
        var customer = StaleCustomer(storedBalance: 0m, storedOverdue: 0m);
        _ledger.GetCurrentBalanceAsync(_customerId, Arg.Any<CancellationToken>()).Returns(0m);
        _reports.GetOpenInvoicesAcrossCustomersAsync(Arg.Any<CancellationToken>()).Returns(new List<OpenInvoiceRow>
        {
            new(_customerId, "Acme", "TRY", 75m, DateTime.UtcNow.AddDays(-3)),
            new(_customerId, "Acme", "TRY", 40m, DateTime.UtcNow.AddDays(5)),
        });

        var result = await _sut.Handle(new RecomputeCustomerBalancesCommand(DryRun: false), default);

        customer.OverdueAmount.Should().Be(75m);
        result.Drifts.Single().ComputedOverdue.Should().Be(75m);
    }

    [Fact]
    public async Task Parity_variance_is_ledger_total_minus_gl_control()
    {
        StaleCustomer(storedBalance: 100m, storedOverdue: 0m);
        _ledger.GetCurrentBalanceAsync(_customerId, Arg.Any<CancellationToken>()).Returns(250m);
        _journals.GetAccountBalancesAsync(null, null, Arg.Any<CancellationToken>()).Returns(new List<AccountBalanceRow>
        {
            new(Guid.NewGuid(), "120", "Alıcılar", 300m, 0m),
        });

        var result = await _sut.Handle(new RecomputeCustomerBalancesCommand(DryRun: true), default);

        result.LedgerTotal.Should().Be(250m);
        result.GlControlBalance.Should().Be(300m);
        result.LedgerVsGlVariance.Should().Be(-50m);
    }

    [Fact]
    public async Task No_drift_does_not_write()
    {
        StaleCustomer(storedBalance: 250m, storedOverdue: 0m);
        _ledger.GetCurrentBalanceAsync(_customerId, Arg.Any<CancellationToken>()).Returns(250m);

        var result = await _sut.Handle(new RecomputeCustomerBalancesCommand(DryRun: false), default);

        result.Drifted.Should().Be(0);
        result.Recomputed.Should().Be(0);
        _customers.DidNotReceive().Update(Arg.Any<Customer>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
