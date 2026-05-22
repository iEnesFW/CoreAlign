using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class GenerateInvoiceFromOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IDocumentSequenceRepository _sequenceRepository = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periodRepository = Substitute.For<IAccountingPeriodRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GenerateInvoiceFromOrderCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public GenerateInvoiceFromOrderCommandHandlerTests()
    {
        // Default behaviour: sequence generates a stable test number; no period
        // restrictions in effect. Individual tests can re-stub these as needed.
        _sequenceRepository
            .ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("INV-TEST-0001");

        _sut = new GenerateInvoiceFromOrderCommandHandler(
            _orderRepository,
            _invoiceRepository,
            _sequenceRepository,
            _periodRepository,
            _unitOfWork);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Closed)]
    public async Task Generates_invoice_for_eligible_status(OrderStatus status)
    {
        var order = BuildOrder(status, quantity: 5, unitPrice: 12m);
        SetupRepositories(order);

        var result = await _sut.Handle(new GenerateInvoiceFromOrderCommand(order.Id), default);

        result.Should().NotBeNull();
        result.Total.Should().Be(60m);
        result.Lines.Should().HaveCount(1);
        result.Status.Should().Be(InvoiceStatus.Issued);
        await _invoiceRepository.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Throws_when_order_status_not_eligible(OrderStatus status)
    {
        var order = BuildOrder(status, quantity: 1, unitPrice: 10m);
        SetupRepositories(order);

        Func<Task> act = () => _sut.Handle(new GenerateInvoiceFromOrderCommand(order.Id), default);

        await act.Should().ThrowAsync<OrderNotEligibleForInvoicingException>();
        await _invoiceRepository.DidNotReceive().AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_invoice_already_exists_for_order()
    {
        var order = BuildOrder(OrderStatus.Shipped, quantity: 1, unitPrice: 10m);
        SetupRepositories(order);
        _invoiceRepository.ExistsForOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => _sut.Handle(new GenerateInvoiceFromOrderCommand(order.Id), default);

        await act.Should().ThrowAsync<InvoiceAlreadyExistsForOrderException>();
    }

    [Fact]
    public async Task Throws_when_order_missing()
    {
        _orderRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        Func<Task> act = () => _sut.Handle(new GenerateInvoiceFromOrderCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    private void SetupRepositories(Order order)
    {
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _invoiceRepository.ExistsForOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(false);
    }

    private static Order BuildOrder(OrderStatus status, decimal quantity, decimal unitPrice)
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "USD")
        {
            Id = OrderId,
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId }
        };
        var line = new OrderLine(ProductId, "SKU-A", "Widget", quantity, unitPrice)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
        order.ReplaceLines(new[] { line });
        AdvanceTo(order, status);
        return order;
    }

    private static void AdvanceTo(Order order, OrderStatus target)
    {
        if (order.Status == target) return;
        var path = target switch
        {
            OrderStatus.Confirmed => new[] { OrderStatus.Confirmed },
            OrderStatus.Shipped => new[] { OrderStatus.Confirmed, OrderStatus.Shipped },
            OrderStatus.Closed => new[] { OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Closed },
            OrderStatus.Cancelled => new[] { OrderStatus.Cancelled },
            _ => Array.Empty<OrderStatus>()
        };
        foreach (var s in path)
        {
            order.ChangeStatus(s);
        }
    }
}
