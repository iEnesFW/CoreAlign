using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Customers;

public class DeleteCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly DeleteCustomerCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    public DeleteCustomerCommandHandlerTests()
    {
        _sut = new DeleteCustomerCommandHandler(_customers, _uow);
    }

    [Fact]
    public async Task Archives_customer_without_physically_removing()
    {
        var customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId };
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _sut.Handle(new DeleteCustomerCommand(CustomerId), default);

        result.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Archived);
        _customers.Received(1).Update(customer);
        _customers.DidNotReceive().Remove(Arg.Any<Customer>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_customer_not_found()
    {
        _customers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        Func<Task> act = () => _sut.Handle(new DeleteCustomerCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<CustomerNotFoundException>();
        _customers.DidNotReceive().Update(Arg.Any<Customer>());
        _customers.DidNotReceive().Remove(Arg.Any<Customer>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
