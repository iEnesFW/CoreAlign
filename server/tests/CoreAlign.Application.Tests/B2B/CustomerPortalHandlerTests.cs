using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class CustomerPortalHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IDealerAccountRepository _dealers = Substitute.For<IDealerAccountRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();

    [Fact]
    public async Task GetOrderById_returns_404_when_order_belongs_to_a_different_customer()
    {
        var callerCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(callerCustomerId);

        var order = BuildOrder(orderId, otherCustomerId, "ORD-OTHER");
        _orders.GetWithLinesAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new GetCustomerPortalOrderByIdHandler(_scope, _orders);

        var act = async () => await handler.Handle(new GetCustomerPortalOrderByIdQuery(orderId), default);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    [Fact]
    public async Task GetOrders_returns_only_callers_customer_orders()
    {
        var callerCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(callerCustomerId);

        // The handler must pass the resolved customer id to SearchAsync; the
        // repository contract is responsible for filtering. We verify the
        // handler never asks the repository for orders that aren't scoped, and
        // that any rows the repo returns are passed through untouched (i.e. no
        // post-filter trickery hides a bug in scoping).
        var myRow = new OrderSearchRow(
            Guid.NewGuid(), "ORD-1", callerCustomerId, "Acme Holding",
            DateTime.UtcNow, OrderStatus.Submitted, "TRY", 100m);
        _orders.SearchAsync(null, callerCustomerId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new[] { myRow }, 1));

        var handler = new GetCustomerPortalOrdersHandler(_scope, _orders);

        var result = await handler.Handle(new GetCustomerPortalOrdersQuery(null, 1, 20), default);

        result.Items.Should().HaveCount(1);
        result.Items.Single().CustomerId.Should().Be(callerCustomerId);

        // The handler must NOT call SearchAsync with a different customer id —
        // any deviation here would mean the scope filter is bypassable.
        await _orders.Received(1).SearchAsync(
            Arg.Any<string?>(),
            callerCustomerId,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await _orders.DidNotReceive().SearchAsync(
            Arg.Any<string?>(),
            otherCustomerId,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInvoiceById_returns_404_when_invoice_belongs_to_a_different_customer()
    {
        var callerCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(callerCustomerId);

        var invoice = BuildInvoice(invoiceId, otherCustomerId, "INV-OTHER");
        _invoices.GetWithLinesAsync(invoiceId, Arg.Any<CancellationToken>()).Returns(invoice);

        var handler = new GetCustomerPortalInvoiceByIdHandler(_scope, _invoices);

        var act = async () => await handler.Handle(new GetCustomerPortalInvoiceByIdQuery(invoiceId), default);

        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }

    [Fact]
    public async Task Dashboard_computes_counts_from_repository_aggregates()
    {
        var customerId = Guid.NewGuid();
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        _orders.GetOrderStatusBreakdownAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new StatusGroup(OrderStatus.Submitted.ToString(), 3, 0m),
                new StatusGroup(OrderStatus.Approved.ToString(), 2, 0m),
                new StatusGroup(OrderStatus.Cancelled.ToString(), 5, 0m),
                new StatusGroup(OrderStatus.Delivered.ToString(), 10, 0m),
            });

        _invoices.GetInvoiceStatusBreakdownAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new StatusGroup(InvoiceStatus.Issued.ToString(), 2, 0m),
                new StatusGroup(InvoiceStatus.Sent.ToString(), 1, 0m),
                new StatusGroup(InvoiceStatus.Paid.ToString(), 7, 0m),
            });

        var openInvoice1 = BuildInvoice(Guid.NewGuid(), customerId, "INV-1", currency: "TRY", total: 100m);
        var openInvoice2 = BuildInvoice(Guid.NewGuid(), customerId, "INV-2", currency: "TRY", total: 250m);
        _invoices.GetOpenForCustomerAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new[] { openInvoice1, openInvoice2 });
        _invoices.GetMonthlyRevenueByCustomerAsync(customerId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new MonthlyInvoiceTotal(2026, 5, 1234m, 4, 0m) });

        var activeDealer = BuildDealer("BAYI-A", "Bayi A", DealerAccountStatus.Active);
        var suspendedDealer = BuildDealer("BAYI-S", "Bayi S", DealerAccountStatus.Suspended);
        _dealers.ListByCustomerAsync(customerId, Arg.Any<CancellationToken>())
            .Returns(new[] { activeDealer, suspendedDealer });

        _orders.SearchAsync(null, customerId, 1, 5, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<OrderSearchRow>(), 0));
        _invoices.SearchAsync(null, customerId, 1, 5, null, false, null, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<InvoiceSearchRow>(), 0));

        var customer = new Customer("Acme Holding") { Id = customerId, TenantId = TenantId };
        _customers.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);

        var handler = new GetCustomerPortalDashboardHandler(_scope, _orders, _invoices, _dealers, _customers);

        var result = await handler.Handle(new GetCustomerPortalDashboardQuery(), default);

        result.CustomerId.Should().Be(customerId);
        result.CustomerName.Should().Be("Acme Holding");
        result.TotalActiveOrders.Should().Be(5);
        result.TotalOpenInvoices.Should().Be(3);
        result.OpenInvoiceTotalAmount.Should().Be(350m);
        result.OpenInvoiceCurrency.Should().Be("TRY");
        result.TotalActiveDealers.Should().Be(1);
        result.RecentOrders.Should().BeEmpty();
        result.RecentInvoices.Should().BeEmpty();
        result.InvoicedLast30DaysAmount.Should().Be(1234m);
        result.InvoicedLast30DaysCurrency.Should().Be("TRY");
    }

    [Fact]
    public async Task GetDealers_returns_only_dealers_linked_to_the_callers_customer()
    {
        var callerCustomerId = Guid.NewGuid();
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(callerCustomerId);

        var dealerA = BuildDealer("BAYI-A", "Bayi A", DealerAccountStatus.Active);
        var dealerB = BuildDealer("BAYI-B", "Bayi B", DealerAccountStatus.Active);
        _dealers.ListByCustomerAsync(callerCustomerId, Arg.Any<CancellationToken>())
            .Returns(new[] { dealerA, dealerB });

        var handler = new GetCustomerPortalDealersHandler(_scope, _dealers);

        var result = await handler.Handle(new GetCustomerPortalDealersQuery(), default);

        result.Should().HaveCount(2);
        result.Select(d => d.Code).Should().BeEquivalentTo(new[] { "BAYI-A", "BAYI-B" });

        // Never queried with a different customer id.
        await _dealers.Received(1).ListByCustomerAsync(callerCustomerId, Arg.Any<CancellationToken>());
    }

    private static Order BuildOrder(Guid id, Guid customerId, string orderNumber)
    {
        var order = new Order(orderNumber, customerId, DateTime.UtcNow, "TRY")
        {
            Id = id,
            TenantId = TenantId,
        };
        return order;
    }

    private static Invoice BuildInvoice(Guid id, Guid customerId, string invoiceNumber, string currency = "TRY", decimal total = 0m)
    {
        var invoice = new Invoice(invoiceNumber, customerId, "Acme Holding", currency)
        {
            Id = id,
            TenantId = TenantId,
        };
        if (total > 0m)
        {
            // Drive Total through a real line + Recalculate so the entity's
            // invariants stay intact (rather than reflecting around private
            // setters).
            invoice.Lines.Add(new InvoiceLine("SKU-FIX", "Fixture", null, quantity: 1m, unitPrice: total));
            invoice.Recalculate();
        }
        return invoice;
    }

    private static DealerAccount BuildDealer(string code, string name, DealerAccountStatus status)
    {
        var dealer = new DealerAccount(code, name, createdByUserId: null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        if (status == DealerAccountStatus.Suspended) dealer.Suspend("test fixture");
        else if (status == DealerAccountStatus.Archived) dealer.Archive();
        return dealer;
    }
}
