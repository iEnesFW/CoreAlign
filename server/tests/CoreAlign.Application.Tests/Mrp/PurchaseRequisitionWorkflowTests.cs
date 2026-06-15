using CoreAlign.Application.B2B;
using CoreAlign.Application.Fx;
using CoreAlign.Application.Mrp;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Purchasing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Mrp;

public class PurchaseRequisitionWorkflowTests
{
    private static PurchaseRequisition Draft(Guid? id = null)
    {
        var req = new PurchaseRequisition(
            number: "PR-2026-00001",
            requestedByUserId: Guid.NewGuid(),
            reason: PurchaseRequisitionReason.Manual,
            notes: "test");
        req.ReplaceLines(new[]
        {
            new PurchaseRequisitionLine(Guid.NewGuid(), "SKU", "Name", 1m, 10m),
        });
        if (id is not null)
        {
            req.Id = id.Value;
        }
        return req;
    }

    [Fact]
    public void Submit_moves_draft_to_Submitted()
    {
        var req = Draft();
        req.Status.Should().Be(PurchaseRequisitionStatus.Draft);

        req.Submit();

        req.Status.Should().Be(PurchaseRequisitionStatus.Submitted);
        req.SubmittedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Approve_moves_Submitted_to_Approved_and_sets_ApprovedByUserId()
    {
        var req = Draft();
        req.Submit();
        var approver = Guid.NewGuid();

        req.Approve(approver);

        req.Status.Should().Be(PurchaseRequisitionStatus.Approved);
        req.ApprovedByUserId.Should().Be(approver);
        req.ApprovedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reject_moves_Submitted_to_Rejected_with_reason()
    {
        var req = Draft();
        req.Submit();

        req.Reject("budget");

        req.Status.Should().Be(PurchaseRequisitionStatus.Rejected);
        req.RejectReason.Should().Be("budget");
        req.RejectedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reject_from_Draft_throws()
    {
        var req = Draft();
        Action act = () => req.Reject("x");
        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    private static PurchaseRequisition ApprovedRequisition(Guid reqId, Guid productId, decimal estimatedUnitCost = 10m)
    {
        var req = new PurchaseRequisition(
            number: "PR-2026-00001",
            requestedByUserId: Guid.NewGuid(),
            reason: PurchaseRequisitionReason.Manual,
            notes: "test")
        {
            Id = reqId,
        };
        req.ReplaceLines(new[]
        {
            new PurchaseRequisitionLine(productId, "SKU", "Name", 1m, estimatedUnitCost),
        });
        req.Submit();
        req.Approve(Guid.NewGuid());
        return req;
    }

    private static Product ProductWithTaxRate(Guid productId, Guid? taxRateId)
    {
        var product = new Product(sku: "SKU", name: "Name", unit: "pcs", price: 10m, currency: "TRY")
        {
            Id = productId,
        };
        if (taxRateId is not null)
        {
            typeof(Product).GetProperty(nameof(Product.TaxRateId))!.SetValue(product, taxRateId);
        }
        return product;
    }

    private static IMediator MediatorReturningPo(Guid poId, Guid vendorId) =>
        BuildMediator(poId, vendorId, out _);

    private static IMediator BuildMediator(Guid poId, Guid vendorId, out List<CreatePurchaseOrderCommand> captured)
    {
        var commands = new List<CreatePurchaseOrderCommand>();
        captured = commands;
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<CreatePurchaseOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                commands.Add(call.Arg<CreatePurchaseOrderCommand>());
                return Task.FromResult(new PurchaseOrderDto(
                    Id: poId,
                    PoNumber: "PO-2026-00001",
                    VendorId: vendorId,
                    VendorName: "Acme",
                    OrderDate: DateTime.UtcNow,
                    ExpectedDate: null,
                    Currency: "TRY",
                    ExchangeRate: 1m,
                    WarehouseId: null,
                    Status: PurchaseOrderStatus.Draft,
                    Subtotal: 10m,
                    TaxTotal: 0m,
                    Total: 10m,
                    Notes: null,
                    Lines: new List<PurchaseOrderLineDto>(),
                    CreatedAtUtc: DateTime.UtcNow));
            });
        return mediator;
    }

    [Fact]
    public async Task ConvertToPurchaseOrder_handler_marks_status_Converted()
    {
        var reqId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var requisition = ApprovedRequisition(reqId, productId);

        var requisitions = Substitute.For<IPurchaseRequisitionRepository>();
        requisitions.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(requisition);

        var vendorId = Guid.NewGuid();
        var vendors = Substitute.For<IVendorRepository>();
        vendors.GetByIdAsync(vendorId, Arg.Any<CancellationToken>())
            .Returns(new Vendor(name: "Acme", code: "V-1") { Id = vendorId });

        var products = Substitute.For<IProductRepository>();
        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [productId] = ProductWithTaxRate(productId, null) });

        var taxRates = Substitute.For<ITaxRateRepository>();

        var expectedPoId = Guid.NewGuid();
        var mediator = MediatorReturningPo(expectedPoId, vendorId);

        var handler = new ConvertRequisitionToPurchaseOrderHandler(
            requisitions,
            vendors,
            products,
            taxRates,
            mediator,
            NullLogger<ConvertRequisitionToPurchaseOrderHandler>.Instance);
        var poId = await handler.Handle(
            new ConvertRequisitionToPurchaseOrderCommand(reqId, vendorId, "TRY"),
            default);

        poId.Should().Be(expectedPoId);
        requisition.Status.Should().Be(PurchaseRequisitionStatus.Converted);
        requisition.ConvertedPurchaseOrderId.Should().Be(poId);
        await mediator.Received(1).Send(Arg.Any<CreatePurchaseOrderCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertToPurchaseOrder_carries_product_tax_rate_onto_po_line()
    {
        var reqId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxRateId = Guid.NewGuid();
        var requisition = ApprovedRequisition(reqId, productId);

        var requisitions = Substitute.For<IPurchaseRequisitionRepository>();
        requisitions.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(requisition);

        var vendorId = Guid.NewGuid();
        var vendors = Substitute.For<IVendorRepository>();
        vendors.GetByIdAsync(vendorId, Arg.Any<CancellationToken>())
            .Returns(new Vendor(name: "Acme", code: "V-1") { Id = vendorId });

        var products = Substitute.For<IProductRepository>();
        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [productId] = ProductWithTaxRate(productId, taxRateId) });

        var taxRates = Substitute.For<ITaxRateRepository>();
        taxRates.GetByIdAsync(taxRateId, Arg.Any<CancellationToken>())
            .Returns(new TaxRate(code: "KDV20", name: "KDV %20", ratePercent: 20m));

        var mediator = BuildMediator(Guid.NewGuid(), vendorId, out var captured);

        var handler = new ConvertRequisitionToPurchaseOrderHandler(
            requisitions,
            vendors,
            products,
            taxRates,
            mediator,
            NullLogger<ConvertRequisitionToPurchaseOrderHandler>.Instance);

        await handler.Handle(new ConvertRequisitionToPurchaseOrderCommand(reqId, vendorId, "TRY"), default);

        captured.Should().HaveCount(1);
        var line = captured[0].Lines.Single();
        line.TaxRatePercent.Should().Be(20m);
    }

    [Fact]
    public async Task ConvertToPurchaseOrder_resolves_exchange_rate_for_foreign_currency()
    {
        var reqId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var requisition = ApprovedRequisition(reqId, productId);

        var requisitions = Substitute.For<IPurchaseRequisitionRepository>();
        requisitions.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(requisition);

        var vendorId = Guid.NewGuid();
        var vendors = Substitute.For<IVendorRepository>();
        vendors.GetByIdAsync(vendorId, Arg.Any<CancellationToken>())
            .Returns(new Vendor(name: "Acme", code: "V-1") { Id = vendorId });

        var products = Substitute.For<IProductRepository>();
        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [productId] = ProductWithTaxRate(productId, null) });

        var taxRates = Substitute.For<ITaxRateRepository>();

        var fxResolver = Substitute.For<IFxRateResolver>();
        fxResolver.ResolveAsync("USD", Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new FxRateSnapshot("USD", BuyingRate: 32.5m, SellingRate: 32.9m, EffectiveDate: DateTime.UtcNow, Source: "TCMB"));

        var mediator = BuildMediator(Guid.NewGuid(), vendorId, out var captured);

        var handler = new ConvertRequisitionToPurchaseOrderHandler(
            requisitions,
            vendors,
            products,
            taxRates,
            mediator,
            NullLogger<ConvertRequisitionToPurchaseOrderHandler>.Instance,
            fxResolver);

        await handler.Handle(new ConvertRequisitionToPurchaseOrderCommand(reqId, vendorId, "USD"), default);

        captured.Should().HaveCount(1);
        captured[0].ExchangeRate.Should().Be(32.5m);
        captured[0].Currency.Should().Be("USD");
    }

    [Fact]
    public async Task ConvertToPurchaseOrder_uses_unit_rate_for_base_currency_without_calling_resolver()
    {
        var reqId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var requisition = ApprovedRequisition(reqId, productId);

        var requisitions = Substitute.For<IPurchaseRequisitionRepository>();
        requisitions.GetByIdAsync(reqId, Arg.Any<CancellationToken>()).Returns(requisition);

        var vendorId = Guid.NewGuid();
        var vendors = Substitute.For<IVendorRepository>();
        vendors.GetByIdAsync(vendorId, Arg.Any<CancellationToken>())
            .Returns(new Vendor(name: "Acme", code: "V-1") { Id = vendorId });

        var products = Substitute.For<IProductRepository>();
        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [productId] = ProductWithTaxRate(productId, null) });

        var taxRates = Substitute.For<ITaxRateRepository>();
        var fxResolver = Substitute.For<IFxRateResolver>();

        var mediator = BuildMediator(Guid.NewGuid(), vendorId, out var captured);

        var handler = new ConvertRequisitionToPurchaseOrderHandler(
            requisitions,
            vendors,
            products,
            taxRates,
            mediator,
            NullLogger<ConvertRequisitionToPurchaseOrderHandler>.Instance,
            fxResolver);

        await handler.Handle(new ConvertRequisitionToPurchaseOrderCommand(reqId, vendorId, "TRY"), default);

        captured.Should().HaveCount(1);
        captured[0].ExchangeRate.Should().Be(1m);
        await fxResolver.DidNotReceive().ResolveAsync(
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
