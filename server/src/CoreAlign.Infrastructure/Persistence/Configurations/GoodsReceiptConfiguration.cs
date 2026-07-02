using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.GrnNumber).HasMaxLength(64).IsRequired();
        builder.Property(g => g.VendorName).HasMaxLength(300).IsRequired();
        builder.Property(g => g.PoNumber).HasMaxLength(64).IsRequired();
        builder.Property(g => g.Status).HasMaxLength(24).HasConversion<string>();
        builder.Property(g => g.Currency).HasMaxLength(3).IsRequired();
        builder.Property(g => g.IdempotencyKey).HasMaxLength(80).IsRequired();
        builder.Property(g => g.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(g => g.Notes).HasMaxLength(2000);
        builder.Property(g => g.ReversalReason).HasMaxLength(500);
        builder.Property(g => g.QcStatus).HasConversion<int>();
        builder.Property(g => g.QcRejectionReason).HasMaxLength(500);
        builder.Property(g => g.ReceiptDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(g => g.ReversedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(g => g.QcDecisionAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(g => g.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(g => g.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(g => g.Vendor).WithMany().HasForeignKey(g => g.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(g => g.PurchaseOrder).WithMany().HasForeignKey(g => g.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(g => g.Warehouse).WithMany().HasForeignKey(g => g.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(g => g.Lines).WithOne(l => l.GoodsReceipt).HasForeignKey(l => l.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => new { g.TenantId, g.GrnNumber }).IsUnique();
        builder.HasIndex(g => new { g.TenantId, g.IdempotencyKey }).IsUnique();
        builder.HasIndex(g => new { g.TenantId, g.PurchaseOrderId });
        builder.HasIndex(g => new { g.TenantId, g.Status });
        builder.HasIndex(g => new { g.TenantId, g.QcStatus });

        builder.Ignore(g => g.TotalCost);
    }
}

public class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.QuantityReceived).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitCost).HasColumnType("numeric(18,4)");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.PurchaseOrderLine).WithMany().HasForeignKey(l => l.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.GoodsReceiptId);
        builder.HasIndex(l => l.PurchaseOrderLineId);

        builder.Ignore(l => l.LineCost);
    }
}
