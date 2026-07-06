using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class StockCostLayerConfiguration : IEntityTypeConfiguration<StockCostLayer>
{
    public void Configure(EntityTypeBuilder<StockCostLayer> builder)
    {
        // DbSet-less entity (accessed via _context.Set<StockCostLayer>()); explicit plural table name
        // so index/pk/fk stems match the hand-authored migration.
        builder.ToTable("stock_cost_layers");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UnitCost).HasColumnType("numeric(18,4)");
        builder.Property(l => l.OriginalQuantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.RemainingQuantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.ReceivedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);

        // Open-layer FIFO lookup: oldest-first, scoped to a stock item. Soft Guid references
        // (ProductId/WarehouseId/StockItemId) mirror the StockMovement ledger convention.
        builder.HasIndex(l => new { l.StockItemId, l.ReceivedAtUtc });
        builder.HasIndex(l => l.SourceMovementId);
    }
}
