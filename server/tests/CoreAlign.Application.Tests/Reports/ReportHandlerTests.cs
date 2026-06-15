using CoreAlign.Application.Reports.Accounting;
using CoreAlign.Application.Reports.Common;
using CoreAlign.Application.Reports.Inventory;
using CoreAlign.Application.Reports.Purchase;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Reports;

public class ReportHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();

    public ReportHandlerTests()
    {
        _tenantContext.RequireTenantId().Returns(_tenantId);
        _tenants.GetByIdAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new Tenant("Acme", "acme") { Id = _tenantId, DefaultCurrency = "TRY", LocaleCode = "tr-TR" });
    }

    [Fact]
    public async Task StockOnHand_handler_aggregates_qty_and_value_into_footer()
    {
        var stockItems = Substitute.For<IStockItemRepository>();
        var warehouseId = Guid.NewGuid();
        var rows = new List<StockItemSearchRow>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Item 1", 10m, 5m, "TRY",
                warehouseId, "WH1", "Main", null, null, null, null, 50m, 0m, 4m, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-2", "Item 2", 10m, 5m, "TRY",
                warehouseId, "WH1", "Main", null, null, null, null, 25m, 0m, 8m, DateTime.UtcNow),
        };
        stockItems.SearchAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var handler = new StockOnHandReportQueryHandler(stockItems, _tenants, _tenantContext);
        var doc = await handler.Handle(new StockOnHandReportQuery(warehouseId, null, false), CancellationToken.None);

        doc.Header.Title.Should().Be("Stock on hand");
        doc.Rows.Should().HaveCount(2);
        doc.FooterTotals.Should().NotBeNull();
        // total qty = 75, total value = 50*4 + 25*8 = 200 + 200 = 400
        doc.FooterTotals![3].Value.Should().Be(75m);
        doc.FooterTotals![7].Value.Should().Be(400m);
    }

    [Fact]
    public async Task StockOnHand_handler_passes_warehouse_filter_to_repository()
    {
        var stockItems = Substitute.For<IStockItemRepository>();
        stockItems.SearchAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<StockItemSearchRow>());
        var warehouseId = Guid.NewGuid();

        var handler = new StockOnHandReportQueryHandler(stockItems, _tenants, _tenantContext);
        await handler.Handle(new StockOnHandReportQuery(warehouseId, null, true), CancellationToken.None);

        await stockItems.Received(1).SearchAsync(
            Arg.Is<Guid?>(g => g == null),
            Arg.Is<Guid?>(g => g == warehouseId),
            Arg.Is<bool>(b => b),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StockMovements_handler_returns_zero_rows_when_repo_empty()
    {
        var movements = Substitute.For<IStockMovementRepository>();
        var empty = (IReadOnlyList<StockMovement>)new List<StockMovement>();
        movements.SearchAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<StockMovementType?>(),
                Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((empty, 0));

        var handler = new StockMovementsReportQueryHandler(movements, _tenants, _tenantContext);
        var doc = await handler.Handle(new StockMovementsReportQuery(
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, null, null, null), CancellationToken.None);

        doc.Rows.Should().BeEmpty();
        doc.Header.Title.Should().Be("Stock movements");
        doc.Header.PeriodFromUtc.Should().NotBeNull();
        doc.Header.PeriodToUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Reorder_handler_only_passes_onlyBelowReorder_true()
    {
        var stockItems = Substitute.For<IStockItemRepository>();
        stockItems.SearchAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<StockItemSearchRow>());

        var handler = new ReorderLevelReportQueryHandler(stockItems, _tenants, _tenantContext);
        await handler.Handle(new ReorderLevelReportQuery(null), CancellationToken.None);

        await stockItems.Received(1).SearchAsync(
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Is<bool>(b => b),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApAging_handler_omits_zero_outstanding_vendors_and_sums_totals()
    {
        var bills = Substitute.For<IVendorBillRepository>();
        var rows = new List<VendorAgingRow>
        {
            new(Guid.NewGuid(), "Vendor A", "TRY", 100m, 50m, 0m, 0m, 0m),
            new(Guid.NewGuid(), "Vendor B", "TRY", 0m, 0m, 0m, 0m, 0m),
            new(Guid.NewGuid(), "Vendor C", "TRY", 0m, 0m, 0m, 0m, 200m),
        };
        bills.GetAgingBucketsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var handler = new ApAgingReportQueryHandler(bills, _tenants, _tenantContext);
        var doc = await handler.Handle(new ApAgingReportQuery(null), CancellationToken.None);

        doc.Rows.Should().HaveCount(2);
        doc.FooterTotals.Should().NotBeNull();
        // current = 100, 1-30 = 50, 90+ = 200, total = 350
        doc.FooterTotals![2].Value.Should().Be(100m);
        doc.FooterTotals![3].Value.Should().Be(50m);
        doc.FooterTotals![6].Value.Should().Be(200m);
        doc.FooterTotals![7].Value.Should().Be(350m);
    }

    [Fact]
    public async Task CashFlow_handler_groups_rows_by_section()
    {
        var reader = Substitute.For<IReportDataReader>();
        var rows = new List<CashFlowRow>
        {
            new(DateTime.UtcNow, "Operating", "Customer receipts", "Cust A", "PAY-1", 500m, "TRY"),
            new(DateTime.UtcNow, "Operating", "Vendor payments", "Vend B", "VP-1", -300m, "TRY"),
        };
        reader.GetCashFlowAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var handler = new CashFlowReportQueryHandler(reader, _tenants, _tenantContext);
        var doc = await handler.Handle(
            new CashFlowReportQuery(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
            CancellationToken.None);

        var groups = doc.Groups!;
        groups.Should().HaveCount(1);
        groups[0].Label.Should().Be("Operating");
        doc.FooterTotals.Should().NotBeNull();
        doc.FooterTotals![5].Value.Should().Be(200m);
    }

    [Fact]
    public async Task GlDetail_handler_groups_by_account_with_running_balance()
    {
        var reader = Substitute.For<IReportDataReader>();
        var accounts = Substitute.For<IGLAccountRepository>();
        var accountId = Guid.NewGuid();
        var lines = new List<GlDetailLineRow>
        {
            new(new DateTime(2026, 1, 1), "JE-1", "REF1", "Sale", "INV-1", accountId, "120", "Receivables", 100m, 0m),
            new(new DateTime(2026, 1, 5), "JE-2", "REF2", "Receipt", "PAY-1", accountId, "120", "Receivables", 0m, 60m),
        };
        reader.GetGlDetailAsync(Arg.Any<Guid?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(lines);

        var handler = new GlDetailReportQueryHandler(reader, accounts, _tenants, _tenantContext);
        var doc = await handler.Handle(new GlDetailReportQuery(null, null, null), CancellationToken.None);

        var groups = doc.Groups!;
        groups.Should().HaveCount(1);
        groups[0].Rows.Should().HaveCount(2);
        groups[0].FooterTotals![7].Value.Should().Be(40m);
    }

    [Fact]
    public async Task PurchaseByVendor_handler_sums_totals()
    {
        var reader = Substitute.For<IReportDataReader>();
        var rows = new List<PurchaseByVendorRow>
        {
            new(Guid.NewGuid(), "Vendor A", "TRY", 2, 1000m, 200m, 1200m),
            new(Guid.NewGuid(), "Vendor B", "TRY", 1, 500m, 100m, 600m),
        };
        reader.GetPurchaseByVendorAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var handler = new PurchaseByVendorReportQueryHandler(reader, _tenants, _tenantContext);
        var doc = await handler.Handle(
            new PurchaseByVendorReportQuery(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
            CancellationToken.None);

        doc.Rows.Should().HaveCount(2);
        doc.FooterTotals.Should().NotBeNull();
        doc.FooterTotals![2].Value.Should().Be(3);
        doc.FooterTotals![5].Value.Should().Be(1800m);
    }

    [Fact]
    public async Task PurchaseByProduct_handler_aggregates_totals()
    {
        var reader = Substitute.For<IReportDataReader>();
        var rows = new List<PurchaseByProductRow>
        {
            new(Guid.NewGuid(), "SKU-1", "Item 1", "TRY", 10m, 100m, 120m),
            new(Guid.NewGuid(), "SKU-2", "Item 2", "TRY", 5m, 50m, 60m),
        };
        reader.GetPurchaseByProductAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var handler = new PurchaseByProductReportQueryHandler(reader, _tenants, _tenantContext);
        var doc = await handler.Handle(
            new PurchaseByProductReportQuery(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
            CancellationToken.None);

        doc.Rows.Should().HaveCount(2);
        doc.FooterTotals![3].Value.Should().Be(15m);
        doc.FooterTotals![5].Value.Should().Be(180m);
    }

    [Fact]
    public async Task OpenPos_handler_renders_age_days_and_total()
    {
        var reader = Substitute.For<IReportDataReader>();
        var rows = new List<OpenPoRow>
        {
            new(Guid.NewGuid(), "PO-1", DateTime.UtcNow.AddDays(-10), null,
                Guid.NewGuid(), "Vendor A", "Approved", "TRY", 1500m, 10),
            new(Guid.NewGuid(), "PO-2", DateTime.UtcNow.AddDays(-3), null,
                Guid.NewGuid(), "Vendor B", "Submitted", "TRY", 500m, 3),
        };
        reader.GetOpenPurchaseOrdersAsync(Arg.Any<CancellationToken>())
            .Returns(rows);

        var handler = new OpenPurchaseOrdersReportQueryHandler(reader, _tenants, _tenantContext);
        var doc = await handler.Handle(new OpenPurchaseOrdersReportQuery(), CancellationToken.None);

        doc.Rows.Should().HaveCount(2);
        doc.FooterTotals![6].Value.Should().Be(2000m);
    }

    [Fact]
    public async Task StockOnHand_handler_requires_tenant_context()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.RequireTenantId().Returns(_ =>
            throw new CoreAlign.Domain.Exceptions.MissingTenantContextException());
        var stockItems = Substitute.For<IStockItemRepository>();

        var handler = new StockOnHandReportQueryHandler(stockItems, _tenants, tenantContext);
        await Assert.ThrowsAsync<CoreAlign.Domain.Exceptions.MissingTenantContextException>(() =>
            handler.Handle(new StockOnHandReportQuery(null, null, false), CancellationToken.None));
    }
}
