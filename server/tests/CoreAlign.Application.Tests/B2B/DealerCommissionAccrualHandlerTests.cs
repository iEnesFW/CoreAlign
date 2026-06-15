using CoreAlign.Application.B2B.EventHandlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.B2B;

public class DealerCommissionAccrualHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid ShipmentId = Guid.NewGuid();
    private static readonly Guid OrderLineId = Guid.NewGuid();

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly IDealerAccountRepository _dealers = Substitute.For<IDealerAccountRepository>();
    private readonly IDealerCustomerLinkRepository _links = Substitute.For<IDealerCustomerLinkRepository>();
    private readonly IDealerCommissionLedgerRepository _ledger = Substitute.For<IDealerCommissionLedgerRepository>();
    private readonly DealerCommissionAccrualHandler _sut;

    public DealerCommissionAccrualHandlerTests()
    {
        _sut = new DealerCommissionAccrualHandler(
            _orders, _shipments, _dealers, _links, _ledger,
            NullLogger<DealerCommissionAccrualHandler>.Instance);
    }

    [Fact]
    public async Task When_order_is_not_a_dealer_order_no_commission_is_posted()
    {
        var order = BuildOrder(originDealerAccountId: null);
        _orders.GetWithLinesAndShipmentsAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(BuildEvent(), default);

        await _ledger.DidNotReceive().AddAsync(Arg.Any<DealerCommissionLedgerEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_entry_already_exists_for_dealer_order_shipment_no_duplicate_is_posted()
    {
        var order = BuildOrder(originDealerAccountId: DealerAccountId);
        _orders.GetWithLinesAndShipmentsAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _ledger.ExistsForOrderAndShipmentAsync(DealerAccountId, OrderId, ShipmentId, Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.Handle(BuildEvent(), default);

        await _ledger.DidNotReceive().AddAsync(Arg.Any<DealerCommissionLedgerEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_dealer_has_default_commission_and_no_link_override_entry_uses_dealer_default()
    {
        var order = BuildOrder(originDealerAccountId: DealerAccountId);
        _orders.GetWithLinesAndShipmentsAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _ledger.ExistsForOrderAndShipmentAsync(DealerAccountId, OrderId, ShipmentId, Arg.Any<CancellationToken>())
            .Returns(false);

        var dealer = BuildDealer(commissionPercent: 5m);
        _dealers.GetByIdAsync(DealerAccountId, Arg.Any<CancellationToken>()).Returns(dealer);
        _links.GetByDealerAndCustomerAsync(DealerAccountId, CustomerId, Arg.Any<CancellationToken>()).Returns((DealerCustomerLink?)null);

        var shipment = BuildShipment(quantity: 2m);
        _shipments.GetWithLinesAsync(ShipmentId, Arg.Any<CancellationToken>()).Returns(shipment);

        DealerCommissionLedgerEntry? captured = null;
        await _ledger.AddAsync(Arg.Do<DealerCommissionLedgerEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.Handle(BuildEvent(), default);

        captured.Should().NotBeNull();
        captured!.DealerAccountId.Should().Be(DealerAccountId);
        captured.OrderId.Should().Be(OrderId);
        captured.ShipmentId.Should().Be(ShipmentId);
        captured.CommissionPercent.Should().Be(5m);
        captured.OrderTotal.Should().Be(200m);
        captured.CommissionAmount.Should().Be(10m);
        captured.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task When_link_has_override_commission_percent_entry_uses_override_over_dealer_default()
    {
        var order = BuildOrder(originDealerAccountId: DealerAccountId);
        _orders.GetWithLinesAndShipmentsAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _ledger.ExistsForOrderAndShipmentAsync(DealerAccountId, OrderId, ShipmentId, Arg.Any<CancellationToken>())
            .Returns(false);

        var dealer = BuildDealer(commissionPercent: 5m);
        _dealers.GetByIdAsync(DealerAccountId, Arg.Any<CancellationToken>()).Returns(dealer);
        var link = new DealerCustomerLink(DealerAccountId, CustomerId, assignedByUserId: null, commissionPercentOverride: 8m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        _links.GetByDealerAndCustomerAsync(DealerAccountId, CustomerId, Arg.Any<CancellationToken>()).Returns(link);

        var shipment = BuildShipment(quantity: 1m);
        _shipments.GetWithLinesAsync(ShipmentId, Arg.Any<CancellationToken>()).Returns(shipment);

        DealerCommissionLedgerEntry? captured = null;
        await _ledger.AddAsync(Arg.Do<DealerCommissionLedgerEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.Handle(BuildEvent(), default);

        captured.Should().NotBeNull();
        captured!.CommissionPercent.Should().Be(8m);
        captured.OrderTotal.Should().Be(100m);
        captured.CommissionAmount.Should().Be(8m);
    }

    [Fact]
    public async Task When_effective_commission_is_zero_no_entry_is_posted()
    {
        var order = BuildOrder(originDealerAccountId: DealerAccountId);
        _orders.GetWithLinesAndShipmentsAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _ledger.ExistsForOrderAndShipmentAsync(DealerAccountId, OrderId, ShipmentId, Arg.Any<CancellationToken>())
            .Returns(false);

        var dealer = BuildDealer(commissionPercent: 0m);
        _dealers.GetByIdAsync(DealerAccountId, Arg.Any<CancellationToken>()).Returns(dealer);
        _links.GetByDealerAndCustomerAsync(DealerAccountId, CustomerId, Arg.Any<CancellationToken>()).Returns((DealerCustomerLink?)null);

        await _sut.Handle(BuildEvent(), default);

        await _ledger.DidNotReceive().AddAsync(Arg.Any<DealerCommissionLedgerEntry>(), Arg.Any<CancellationToken>());
    }

    private static OrderShippedEvent BuildEvent() => new(
        TenantId: TenantId,
        OrderId: OrderId,
        ShipmentId: ShipmentId,
        OrderNumber: "ORD-1",
        ShipmentNumber: "SHIP-1",
        IsPartialShipment: false,
        OccurredAtUtc: DateTime.UtcNow);

    private static Order BuildOrder(Guid? originDealerAccountId)
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY")
        {
            Id = OrderId,
            TenantId = TenantId,
        };
        var line = new OrderLine(Guid.NewGuid(), "SKU", "Item", quantity: 10m, unitPrice: 100m)
        {
            Id = OrderLineId,
            TenantId = TenantId,
        };
        line.SetLineNumber(1);
        order.Lines.Add(line);
        if (originDealerAccountId.HasValue)
        {
            order.MarkOrigin("dealer", customerUserId: null, dealerAccountId: originDealerAccountId, dealerUserId: Guid.NewGuid());
        }
        return order;
    }

    private static DealerAccount BuildDealer(decimal commissionPercent) =>
        new("BAYI", "Demo Bayi", createdByUserId: null, commissionPercent: commissionPercent)
        {
            Id = DealerAccountId,
            TenantId = TenantId,
        };

    private static Shipment BuildShipment(decimal quantity)
    {
        var shipment = new Shipment("SHIP-1", OrderId, CustomerId, Guid.NewGuid(), shippingAddressSnapshot: null)
        {
            Id = ShipmentId,
            TenantId = TenantId,
        };
        var sl = new ShipmentLine(OrderLineId, productId: Guid.NewGuid(), productSku: "SKU", productName: "Item", quantity, unitCostSnapshot: 0m)
        {
            TenantId = TenantId,
        };
        shipment.Lines.Add(sl);
        return shipment;
    }
}
