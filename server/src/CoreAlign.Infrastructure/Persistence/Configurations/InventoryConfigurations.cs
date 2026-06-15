using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class StockReasonCodeConfiguration : IEntityTypeConfiguration<StockReasonCode>
{
    public void Configure(EntityTypeBuilder<StockReasonCode> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).HasMaxLength(32).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.Category });
    }
}

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.LotNumber).HasMaxLength(64).IsRequired();
        builder.Property(l => l.SupplierLotRef).HasMaxLength(64);
        builder.Property(l => l.CountryOfOrigin).HasMaxLength(3);
        builder.Property(l => l.BlockReason).HasMaxLength(500);
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.ManufactureDate).HasColumnType("timestamp with time zone");
        builder.Property(l => l.ExpiryDate).HasColumnType("timestamp with time zone");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.TenantId, l.ProductId, l.LotNumber }).IsUnique();
        builder.HasIndex(l => new { l.TenantId, l.ExpiryDate });
    }
}

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.OnHand).HasColumnType("numeric(18,4)");
        builder.Property(s => s.Reserved).HasColumnType("numeric(18,4)");
        builder.Property(s => s.AvgCost).HasColumnType("numeric(18,4)");
        builder.Property(s => s.BinLocation).HasMaxLength(64);
        builder.Property(s => s.LastMovementAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(s => s.ConcurrencyToken)
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        builder.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Warehouse).WithMany().HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Lot).WithMany().HasForeignKey(s => s.LotId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.TenantId, s.ProductId, s.WarehouseId, s.LotId })
            .IsUnique()
            .HasDatabaseName("ix_stock_items_tenant_product_warehouse_lot_unique");
        builder.HasIndex(s => new { s.TenantId, s.WarehouseId });
        builder.HasIndex(s => new { s.TenantId, s.ProductId });

        builder.Ignore(s => s.AvailableToPromise);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(m => m.SourceDocumentType).HasConversion<string>().HasMaxLength(30);
        builder.Property(m => m.SourceReference).HasMaxLength(64);
        builder.Property(m => m.SerialNumber).HasMaxLength(64);
        builder.Property(m => m.Notes).HasMaxLength(1000);
        builder.Property(m => m.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(m => m.UnitCost).HasColumnType("numeric(18,4)");
        builder.Property(m => m.TotalCost).HasColumnType("numeric(18,4)");
        builder.Property(m => m.OnHandAfter).HasColumnType("numeric(18,4)");
        builder.Property(m => m.AvgCostAfter).HasColumnType("numeric(18,4)");
        builder.Property(m => m.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Warehouse).WithMany().HasForeignKey(m => m.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Lot).WithMany().HasForeignKey(m => m.LotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.ReasonCode).WithMany().HasForeignKey(m => m.ReasonCodeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.TenantId, m.OccurredAtUtc }).IsDescending(false, true);
        builder.HasIndex(m => new { m.TenantId, m.ProductId, m.OccurredAtUtc }).IsDescending(false, false, true);
        builder.HasIndex(m => new { m.TenantId, m.WarehouseId, m.OccurredAtUtc }).IsDescending(false, false, true);
        builder.HasIndex(m => new { m.TenantId, m.SourceDocumentType, m.SourceDocumentId });
    }
}

public class StockAllocationConfiguration : IEntityTypeConfiguration<StockAllocation>
{
    public void Configure(EntityTypeBuilder<StockAllocation> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(a => a.QuantityConsumed).HasColumnType("numeric(18,4)");
        builder.Property(a => a.AllocatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.ReleasedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(a => a.Product).WithMany().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Warehouse).WithMany().HasForeignKey(a => a.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Lot).WithMany().HasForeignKey(a => a.LotId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.OrderId });
        builder.HasIndex(a => new { a.TenantId, a.OrderLineId });
        builder.HasIndex(a => new { a.TenantId, a.ProductId, a.Status });

        builder.Ignore(a => a.Remaining);
    }
}
