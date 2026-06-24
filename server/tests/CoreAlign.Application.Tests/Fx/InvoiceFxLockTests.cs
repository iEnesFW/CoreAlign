using CoreAlign.Application.EInvoice;
using CoreAlign.Application.Fx;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Fx;

public class InvoiceFxLockTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerAddressRepository _addresses = Substitute.For<ICustomerAddressRepository>();
    private readonly IPaymentTermRepository _paymentTerms = Substitute.For<IPaymentTermRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IEInvoiceSubmissionOutbox _einvoice = Substitute.For<IEInvoiceSubmissionOutbox>();
    private readonly IFxRateResolverDetailed _fxResolver = Substitute.For<IFxRateResolverDetailed>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();
    private readonly CreateStandaloneInvoiceCommandHandler _sut;

    public InvoiceFxLockTests()
    {
        _sequences
            .ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("INV-FXLOCK-0001");
        _products
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());
        _tenantContext.CurrentTenantId.Returns(TenantId);

        _sut = new CreateStandaloneInvoiceCommandHandler(
            _customers, _addresses, _paymentTerms, _invoices, _products, _sequences,
            _periods, _uow, _email, _einvoice,
            new CoreAlign.Application.CustomerPortal.Credit.CreditLimitGuard(_ledger),
            _fxResolver, _tenantContext);
    }

    [Fact]
    public async Task Issue_snapshots_fx_rate_from_resolver_for_non_try_currency()
    {
        var customer = BuildCustomer();
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var issueDate = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc);
        var fxSnapshot = new FxRateSnapshot("USD", 32.45m, 32.45m, issueDate, FxSourceCodes.Tcmb);
        _fxResolver.ResolveDetailedAsync("USD", issueDate, TenantId, Arg.Any<CancellationToken>())
            .Returns(new FxResolutionResult(fxSnapshot, FxSource.Tcmb, false));

        Invoice? captured = null;
        await _invoices.AddAsync(Arg.Do<Invoice>(i => captured = i), Arg.Any<CancellationToken>());

        var cmd = new CreateStandaloneInvoiceCommand(
            CustomerId: CustomerId,
            IssueDate: issueDate,
            DueDays: 30,
            Currency: "USD",
            Lines: new List<StandaloneInvoiceLineInput>
            {
                new(null, "EXPORT-01", "Export Goods", null, 1m, 100m),
            });

        await _sut.Handle(cmd, default);

        captured.Should().NotBeNull();
        captured!.FxRateSnapshot.Should().Be(32.45m);
        captured.FxSource.Should().Be(FxSourceCodes.Tcmb);
        captured.FxLockedAtUtc.Should().NotBeNull();
        captured.ExchangeRate.Should().Be(32.45m);
    }

    [Fact]
    public async Task Issue_does_not_call_resolver_when_currency_is_base_try()
    {
        var customer = BuildCustomer();
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        Invoice? captured = null;
        await _invoices.AddAsync(Arg.Do<Invoice>(i => captured = i), Arg.Any<CancellationToken>());

        var cmd = new CreateStandaloneInvoiceCommand(
            CustomerId: CustomerId,
            IssueDate: DateTime.UtcNow,
            DueDays: 14,
            Currency: "TRY",
            Lines: new List<StandaloneInvoiceLineInput>
            {
                new(null, "DOMESTIC", "Yurtici", null, 1m, 50m),
            });

        await _sut.Handle(cmd, default);

        captured!.FxRateSnapshot.Should().BeNull();
        captured.FxSource.Should().BeNull();
        captured.FxLockedAtUtc.Should().BeNull();
        await _fxResolver.DidNotReceive().ResolveDetailedAsync(
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Locked_snapshot_is_immutable_when_resolver_subsequently_returns_different_rate()
    {
        var customer = BuildCustomer();
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var issueDate = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc);
        var fxAtIssue = new FxRateSnapshot("USD", 32.45m, 32.45m, issueDate, FxSourceCodes.Tcmb);
        _fxResolver.ResolveDetailedAsync("USD", issueDate, TenantId, Arg.Any<CancellationToken>())
            .Returns(new FxResolutionResult(fxAtIssue, FxSource.Tcmb, false));

        Invoice? captured = null;
        await _invoices.AddAsync(Arg.Do<Invoice>(i => captured = i), Arg.Any<CancellationToken>());

        var cmd = new CreateStandaloneInvoiceCommand(
            CustomerId: CustomerId,
            IssueDate: issueDate,
            DueDays: 30,
            Currency: "USD",
            Lines: new List<StandaloneInvoiceLineInput>
            {
                new(null, "EXPORT-LOCK", "Locked rate", null, 1m, 100m),
            });

        await _sut.Handle(cmd, default);

        captured.Should().NotBeNull();
        var lockedRate = captured!.FxRateSnapshot;
        var lockedSource = captured.FxSource;
        var lockedAt = captured.FxLockedAtUtc;

        var newRate = new FxRateSnapshot("USD", 40.00m, 40.00m, issueDate.AddDays(7), FxSourceCodes.Ecb);
        _fxResolver.ResolveDetailedAsync("USD", Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new FxResolutionResult(newRate, FxSource.Ecb, false));

        captured.FxRateSnapshot.Should().Be(lockedRate);
        captured.FxRateSnapshot.Should().Be(32.45m);
        captured.FxSource.Should().Be(lockedSource);
        captured.FxLockedAtUtc.Should().Be(lockedAt);
        captured.ExchangeRate.Should().Be(32.45m);
    }

    [Fact]
    public async Task Issue_skips_lock_when_resolver_returns_null_but_invoice_still_persists()
    {
        var customer = BuildCustomer();
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var issueDate = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc);
        _fxResolver.ResolveDetailedAsync("USD", issueDate, TenantId, Arg.Any<CancellationToken>())
            .Returns((FxResolutionResult?)null);

        Invoice? captured = null;
        await _invoices.AddAsync(Arg.Do<Invoice>(i => captured = i), Arg.Any<CancellationToken>());

        var cmd = new CreateStandaloneInvoiceCommand(
            CustomerId: CustomerId,
            IssueDate: issueDate,
            DueDays: 14,
            Currency: "USD",
            Lines: new List<StandaloneInvoiceLineInput>
            {
                new(null, "EXPORT-NULL-FX", "No FX", null, 1m, 100m),
            });

        await _sut.Handle(cmd, default);

        captured.Should().NotBeNull();
        captured!.FxRateSnapshot.Should().BeNull();
        captured.FxSource.Should().BeNull();
        captured.ExchangeRate.Should().Be(1m);
    }

    private static Customer BuildCustomer() => new(
        name: "FxLock Customer",
        type: CustomerType.Business,
        code: "FX-1",
        legalName: "FxLock Customer A.Ş.",
        email: null)
    {
        Id = CustomerId,
        TenantId = TenantId,
    };
}
