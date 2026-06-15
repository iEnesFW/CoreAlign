using CoreAlign.Application.B2B;
using CoreAlign.Application.CustomerPortal.Credit;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.CustomerPortal;

public class PortalCreditSnapshotTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();

    public PortalCreditSnapshotTests()
    {
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
    }

    [Fact]
    public void Build_marks_soft_limit_at_eighty_percent_usage()
    {
        var customer = new Customer("ACME");
        SetCreditLimit(customer, 1000m);

        var snapshot = CreditSnapshotFactory.Build(customer, 800m);

        snapshot.IsSoftLimitReached.Should().BeTrue();
        snapshot.IsHardLimitReached.Should().BeFalse();
        snapshot.UsagePercent.Should().Be(80m);
        snapshot.Available.Should().Be(200m);
    }

    [Fact]
    public void Build_marks_hard_limit_at_one_hundred_percent_usage()
    {
        var customer = new Customer("ACME");
        SetCreditLimit(customer, 1000m);

        var snapshot = CreditSnapshotFactory.Build(customer, 1100m);

        snapshot.IsHardLimitReached.Should().BeTrue();
        snapshot.Available.Should().Be(0m);
        snapshot.UsagePercent.Should().Be(110m);
    }

    [Fact]
    public void Build_returns_zero_usage_when_no_limit_is_configured()
    {
        var customer = new Customer("ACME");

        var snapshot = CreditSnapshotFactory.Build(customer, 500m);

        snapshot.Limit.Should().Be(0m);
        snapshot.UsagePercent.Should().Be(0m);
        snapshot.IsSoftLimitReached.Should().BeFalse();
        snapshot.IsHardLimitReached.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_returns_snapshot_from_ledger_balance()
    {
        var customer = new Customer("ACME");
        SetCreditLimit(customer, 5000m);
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);
        _ledger.GetCurrentBalanceAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(2000m);

        var handler = new GetPortalCreditSnapshotHandler(_scope, _customers, _ledger);
        var result = await handler.Handle(new GetPortalCreditSnapshotQuery(), default);

        result.Limit.Should().Be(5000m);
        result.Outstanding.Should().Be(2000m);
        result.Available.Should().Be(3000m);
        result.UsagePercent.Should().Be(40m);
        result.IsSoftLimitReached.Should().BeFalse();
        result.IsHardLimitReached.Should().BeFalse();
    }

    private static void SetCreditLimit(Customer customer, decimal limit)
    {
        customer.Update(
            type: customer.Type,
            name: customer.Name,
            legalName: customer.LegalName,
            tradeName: customer.TradeName,
            nationalId: customer.NationalId,
            taxNumber: customer.TaxNumber,
            taxOffice: customer.TaxOffice,
            email: customer.Email,
            phone: customer.Phone,
            website: customer.Website,
            defaultCurrency: customer.DefaultCurrency,
            paymentTermsId: customer.PaymentTermsId,
            priceListId: customer.PriceListId,
            customerGroupId: customer.CustomerGroupId,
            salesRepUserId: customer.SalesRepUserId,
            creditLimit: limit,
            defaultDiscountPercent: customer.DefaultDiscountPercent,
            classification: customer.Classification,
            channel: customer.Channel,
            territory: customer.Territory,
            languageCode: customer.LanguageCode,
            parentCustomerId: customer.ParentCustomerId,
            notes: customer.Notes);
    }
}
