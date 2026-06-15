using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;

namespace CoreAlign.Infrastructure.Mrp;

/// <summary>
/// Builds a small but rich MRP scenario for the current tenant so the planning
/// workbench has demand, supply, stock and a Make/BOM to chew on. Mirrors the
/// integration-test seeders (SeedReorderProductAsync / SeedMakeWithBuyComponentAsync)
/// but produces a connected dataset: independent demand from a sales order whose
/// lines are allocated-but-unshipped, scheduled receipts from an open purchase
/// order, on-hand stock (some below safety), and a Make finished good exploding
/// into two Buy components via <see cref="ProductComponent"/>.
///
/// Every run stamps a unique tag into SKUs/codes, so repeated calls never collide
/// on unique constraints. TenantId is auto-stamped by the DbContext from the
/// ambient <see cref="ITenantContext"/>, so this only runs under an authenticated
/// tenant request.
/// </summary>
public sealed class MrpDemoSeeder : IMrpDemoSeeder
{
    private readonly CoreAlignDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MrpDemoSeeder(CoreAlignDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<MrpDemoSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var now = DateTime.UtcNow;
        var tag = $"{now:yyMMddHHmmss}-{Guid.NewGuid():N}"[..18];

        var warehouse = new Warehouse($"MRP-{tag}"[..14], $"MRP Demo Depo {tag}");
        _db.Warehouses.Add(warehouse);

        // --- Work center for Rough-Cut Capacity Planning (T6) ---
        // One assembly line with a single 8-hour shift (480 min/day). The Make
        // cabinet routes here below so the capacity workbench shows real load.
        var assemblyLine = new WorkCenter($"ASSY-{tag}"[..14], $"Montaj Hattı {tag}", 480m);
        _db.Set<WorkCenter>().Add(assemblyLine);

        // --- Buy items below safety stock (drive Release / BelowSafetyStock actions) ---
        var screw = BuildBuyProduct(tag, "SCRW", "Çelik Vida M6", safetyStock: 60m, reorderPoint: 120m,
            minStock: 40m, maxStock: 400m, leadTimeDays: 7, standardCost: 4m);
        var bolt = BuildBuyProduct(tag, "BOLT", "Galvaniz Cıvata M8", safetyStock: 80m, reorderPoint: 160m,
            minStock: 50m, maxStock: 500m, leadTimeDays: 10, standardCost: 6m);

        // --- Buy components consumed by the Make assembly (explosion + pegging) ---
        var sheet = BuildBuyProduct(tag, "SHEET", "Sac Levha 2mm", safetyStock: 20m, reorderPoint: 50m,
            minStock: 10m, maxStock: 200m, leadTimeDays: 14, standardCost: 45m);
        var hinge = BuildBuyProduct(tag, "HINGE", "Menteşe Seti", safetyStock: 30m, reorderPoint: 70m,
            minStock: 15m, maxStock: 250m, leadTimeDays: 5, standardCost: 12m);

        // --- A buy item that is healthy but carries an open scheduled receipt ---
        var seal = BuildBuyProduct(tag, "SEAL", "Conta Seti", safetyStock: 25m, reorderPoint: 60m,
            minStock: 15m, maxStock: 220m, leadTimeDays: 6, standardCost: 8m);

        // --- The Make finished good with a 2-component BOM ---
        var cabinet = BuildMakeProduct(tag, "CAB", "Pano Kabini (Montaj)", safetyStock: 5m, reorderPoint: 10m,
            minStock: 2m, maxStock: 40m, leadTimeDays: 3, standardCost: 180m);

        var buyProducts = new[] { screw, bolt, sheet, hinge, seal };
        foreach (var p in buyProducts)
        {
            _db.Products.Add(p);
        }
        _db.Products.Add(cabinet);
        await _db.SaveChangesAsync(cancellationToken);

        // BOM: 1 cabinet = 2 sheets + 4 hinges.
        _db.ProductComponents.Add(new ProductComponent(cabinet.Id, sheet.Id, 2m, "Demo BOM"));
        _db.ProductComponents.Add(new ProductComponent(cabinet.Id, hinge.Id, 4m, "Demo BOM"));

        // Route the Make cabinet to the assembly line at 30 min/unit so the RCCP
        // workbench computes a real work-center load (and overload when demand spikes).
        cabinet.SetRouting(assemblyLine.Id, 30m);
        await _db.SaveChangesAsync(cancellationToken);

        // On-hand stock: screw/bolt deliberately BELOW safety; the rest healthy so
        // gross requirements come from the BOM + sales demand, not just safety stock.
        var onHandByProduct = new (Product Product, decimal OnHand, decimal UnitCost)[]
        {
            (screw, 15m, screw.StandardCost),    // below safety (60)
            (bolt, 25m, bolt.StandardCost),      // below safety (80)
            (sheet, 120m, sheet.StandardCost),   // healthy
            (hinge, 200m, hinge.StandardCost),   // healthy
            (seal, 90m, seal.StandardCost),      // healthy
            (cabinet, 3m, cabinet.StandardCost), // at safety; sales demand pulls it negative
        };
        foreach (var (product, onHand, unitCost) in onHandByProduct)
        {
            var stockItem = new StockItem(product.Id, warehouse.Id);
            stockItem.ApplyReceipt(onHand, unitCost, now);
            _db.StockItems.Add(stockItem);
        }
        await _db.SaveChangesAsync(cancellationToken);

        // --- Supplier + open purchase order (scheduled receipt for one buy item) ---
        var vendor = new Vendor($"MRP Demo Tedarikçi {tag}", VendorType.Business, code: $"VND-{tag}"[..14]);
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(cancellationToken);

        var po = new PurchaseOrder($"PO-MRP-{tag}"[..18], vendor.Id, vendor.Name, now.AddDays(-2), "TRY");
        po.UpdateHeader(vendor.Id, vendor.Name, now.AddDays(-2), now.AddDays(seal.LeadTimeDays),
            "TRY", 1m, warehouse.Id, "MRP demo scheduled receipt");
        po.ReplaceLines(new[]
        {
            new PurchaseOrderLine(seal.Id, seal.Sku, seal.Name, 150m, seal.StandardCost, 20m),
        });
        po.Submit();
        po.Approve(Guid.NewGuid());
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(cancellationToken);

        // --- Customer + open sales order (independent demand) ---
        var customer = new Customer($"MRP Demo Müşteri {tag}", CustomerType.Business, code: $"CST-{tag}"[..14]);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        var order = new Order($"SO-MRP-{tag}"[..18], customer.Id, now.AddDays(-1), "TRY", "MRP demo demand");
        // The planning loader reads independent demand off OrderLines where
        // QuantityAllocated > QuantityShipped and the line is not Shipped/Invoiced/
        // Cancelled. We drive the order to Approved (a real open order) and record
        // line allocation directly. We intentionally stop at Approved rather than
        // calling MarkAllocated, so no OrderAllocationRequested / stock side effects
        // fire — the demand is purely planning-visible, with no warehouse coupling.
        order.UpdateDetails(
            type: OrderType.Standard, source: OrderSource.Manual,
            requestedDeliveryDate: now.AddDays(12), promisedDeliveryDate: now.AddDays(12),
            billingAddressId: null, shippingAddressId: null, paymentTermsId: null, priceListId: null,
            exchangeRate: 1m, shippingCost: 0m, headerDiscountPercent: 0m, headerDiscountAmount: 0m,
            salesRepUserId: null, channel: "MRP-DEMO", internalNotes: null, customerNotes: null,
            originOrderId: null);

        var cabinetLine = new OrderLine(cabinet.Id, cabinet.Sku, cabinet.Name, 10m, cabinet.Price);
        var screwLine = new OrderLine(screw.Id, screw.Sku, screw.Name, 200m, screw.Price);
        order.ReplaceLines(new[] { cabinetLine, screwLine });
        order.Submit();
        order.Approve(Guid.NewGuid());

        // Allocate the full quantity, nothing shipped → QuantityAllocated > QuantityShipped.
        cabinetLine.RecordAllocation(10m);
        screwLine.RecordAllocation(200m);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return new MrpDemoSeedResult(
            TenantId: tenantId,
            ScenarioTag: tag,
            Warehouses: 1,
            Products: buyProducts.Length + 1,
            BuyProducts: buyProducts.Length,
            MakeProducts: 1,
            BomComponents: 2,
            StockItems: onHandByProduct.Length,
            PurchaseOrders: 1,
            PurchaseOrderLines: po.Lines.Count,
            SalesOrders: 1,
            SalesOrderLines: order.Lines.Count);
    }

    private static Product BuildBuyProduct(
        string tag, string skuPrefix, string name,
        decimal safetyStock, decimal reorderPoint, decimal minStock, decimal maxStock,
        int leadTimeDays, decimal standardCost)
    {
        var product = BuildPlanningProduct(tag, skuPrefix, name, safetyStock, reorderPoint,
            minStock, maxStock, leadTimeDays, standardCost);
        product.SetProcurementType(ProcurementType.Buy);
        product.SetPlanningPolicy(LotSizingPolicy.MinMax, 0m, 0m, 0m, 0m, 0m, 0m);
        return product;
    }

    private static Product BuildMakeProduct(
        string tag, string skuPrefix, string name,
        decimal safetyStock, decimal reorderPoint, decimal minStock, decimal maxStock,
        int leadTimeDays, decimal standardCost)
    {
        var product = BuildPlanningProduct(tag, skuPrefix, name, safetyStock, reorderPoint,
            minStock, maxStock, leadTimeDays, standardCost);
        product.SetProcurementType(ProcurementType.Make);
        product.SetPlanningPolicy(LotSizingPolicy.LotForLot, 0m, 0m, 0m, 0m, 0m, 0m);
        return product;
    }

    private static Product BuildPlanningProduct(
        string tag, string skuPrefix, string name,
        decimal safetyStock, decimal reorderPoint, decimal minStock, decimal maxStock,
        int leadTimeDays, decimal standardCost)
    {
        var sku = $"{skuPrefix}-{tag}"[..Math.Min(20, skuPrefix.Length + 1 + tag.Length)];
        var price = Math.Round(standardCost * 1.6m, 2);
        var product = new Product(sku, name, "pcs", price, "TRY");
        product.Update(
            sku: sku, barcode: null, mpn: null, name: name,
            shortDescription: null, description: null, slug: null,
            brandId: null, categoryId: null, parentProductId: null,
            variantAttributesJson: null, tagsJson: null,
            unit: "pcs", baseUomId: null, purchaseUomId: null, salesUomId: null,
            listPrice: price, price: price, minSellingPrice: 0m,
            standardCost: standardCost, currency: "TRY", taxRateId: null, isPriceTaxInclusive: false,
            isStockTracked: true, isLotTracked: false, isSerialTracked: false,
            minStock: minStock, maxStock: maxStock, reorderPoint: reorderPoint,
            safetyStock: safetyStock, leadTimeDays: leadTimeDays,
            weightKg: null, widthCm: null, heightCm: null, depthCm: null, volumeM3: null,
            status: ProductStatus.Active, launchDate: null, endOfLifeDate: null);
        return product;
    }
}
