using CoreAlign.Application.BI;
using CoreAlign.Application.BI.DataSources;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.BI.DataSources;

public sealed class InventoryDataSource : IBIDataSourceAggregator
{
    private readonly CoreAlignDbContext _db;

    public InventoryDataSource(CoreAlignDbContext db)
    {
        _db = db;
    }

    public BIDataSource Source => BIDataSource.Inventory;

    public async Task<BIResultDto> ExecuteAsync(BIQueryConfigDto config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        var bucketed = await _db.StockItems.AsNoTracking()
            .GroupBy(s => s.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                DistinctProducts = g.Select(x => x.ProductId).Distinct().Count(),
                OnHandTotal = g.Sum(x => x.OnHand),
                AllocatedTotal = g.Sum(x => x.Reserved),
                AvailableTotal = g.Sum(x => x.OnHand - x.Reserved),
            })
            .ToListAsync(cancellationToken);

        var rows = bucketed
            .Select(b => (IDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["warehouseId"] = b.WarehouseId,
                ["distinctProducts"] = b.DistinctProducts,
                ["onHandTotal"] = b.OnHandTotal,
                ["allocatedTotal"] = b.AllocatedTotal,
                ["availableTotal"] = b.AvailableTotal,
            })
            .ToList();

        var columns = new List<BIResultColumnDto>
        {
            new("warehouseId", "Warehouse", "guid"),
            new("distinctProducts", "Distinct Products", "integer"),
            new("onHandTotal", "On Hand", "decimal"),
            new("allocatedTotal", "Allocated", "decimal"),
            new("availableTotal", "Available", "decimal"),
        };
        return new BIResultDto(columns, rows, rows.Count);
    }
}
