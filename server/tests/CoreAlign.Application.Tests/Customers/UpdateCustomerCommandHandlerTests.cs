using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Customers;

public class UpdateCustomerCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerTagLinkRepository _tagLinks = Substitute.For<ICustomerTagLinkRepository>();
    private readonly ITagRepository _tags = Substitute.For<ITagRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateCustomerCommandHandler _sut;

    public UpdateCustomerCommandHandlerTests()
    {
        _sut = new UpdateCustomerCommandHandler(_customers, _tagLinks, _tags, _unitOfWork);
    }

    [Fact]
    public async Task Throws_when_customer_is_anonymized()
    {
        var customer = new Customer("Acme") { Id = Guid.NewGuid(), TenantId = TenantId };
        customer.Anonymize("[Silinmiş Müşteri-1]");
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var command = new UpdateCustomerCommand(
            Id: customer.Id,
            Name: "Whatever",
            Type: CustomerType.Business,
            LegalName: null,
            TradeName: null,
            NationalId: null,
            TaxNumber: null,
            TaxOffice: null,
            Email: null,
            Phone: null,
            Website: null,
            DefaultCurrency: "TRY",
            PaymentTermsId: null,
            PriceListId: null,
            CustomerGroupId: null,
            SalesRepUserId: null,
            CreditLimit: 0m,
            DefaultDiscountPercent: 0m,
            Classification: null,
            Channel: null,
            Territory: null,
            LanguageCode: null,
            ParentCustomerId: null,
            Notes: null,
            Status: CustomerStatus.Archived,
            TagIds: null);

        Func<Task> act = () => _sut.Handle(command, default);

        await act.Should().ThrowAsync<CustomerIsAnonymizedException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
