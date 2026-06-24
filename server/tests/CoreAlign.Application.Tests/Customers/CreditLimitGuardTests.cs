using CoreAlign.Application.CustomerPortal.Credit;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Customers;

public class CreditLimitGuardTests
{
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();
    private readonly CreditLimitGuard _sut;

    private static readonly Guid CustomerId = Guid.NewGuid();

    public CreditLimitGuardTests()
    {
        _sut = new CreditLimitGuard(_ledger);
    }

    [Fact]
    public async Task Throws_when_projected_balance_exceeds_hard_limit()
    {
        var customer = BuildCustomer(creditLimit: 1000m);
        _ledger.GetCurrentBalanceAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(800m);

        Func<Task> act = () => _sut.EnsureWithinLimitAsync(customer, additionalExposure: 500m);

        await act.Should().ThrowAsync<CreditLimitExceededException>();
    }

    [Fact]
    public async Task Passes_when_no_limit_configured()
    {
        var customer = BuildCustomer(creditLimit: 0m);
        _ledger.GetCurrentBalanceAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(50_000m);

        Func<Task> act = () => _sut.EnsureWithinLimitAsync(customer, additionalExposure: 999_999m);

        await act.Should().NotThrowAsync();
        await _ledger.DidNotReceive().GetCurrentBalanceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_when_projected_balance_within_limit()
    {
        var customer = BuildCustomer(creditLimit: 1000m);
        _ledger.GetCurrentBalanceAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(300m);

        Func<Task> act = () => _sut.EnsureWithinLimitAsync(customer, additionalExposure: 400m);

        await act.Should().NotThrowAsync();
    }

    private static Customer BuildCustomer(decimal creditLimit)
    {
        var customer = new Customer("Acme", defaultCurrency: "TRY") { Id = CustomerId };
        customer.Update(
            type: CustomerType.Business,
            name: "Acme",
            legalName: null,
            tradeName: null,
            nationalId: null,
            taxNumber: null,
            taxOffice: null,
            email: null,
            phone: null,
            website: null,
            defaultCurrency: "TRY",
            paymentTermsId: null,
            priceListId: null,
            customerGroupId: null,
            salesRepUserId: null,
            creditLimit: creditLimit,
            defaultDiscountPercent: 0m,
            classification: null,
            channel: null,
            territory: null,
            languageCode: null,
            parentCustomerId: null,
            notes: null);
        return customer;
    }
}
