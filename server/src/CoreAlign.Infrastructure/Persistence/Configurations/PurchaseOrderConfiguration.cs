using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PoNumber).HasMaxLength(64).IsRequired();
        builder.Property(p => p.VendorName).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Status).HasMaxLength(24).HasConversion<string>();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(p => p.Subtotal).HasColumnType("numeric(18,4)");
        builder.Property(p => p.TaxTotal).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Total).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.CancelReason).HasMaxLength(500);
        builder.Property(p => p.OrderDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ExpectedDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.SubmittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ApprovedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CancelledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(p => p.Vendor).WithMany().HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Lines).WithOne(l => l.PurchaseOrder).HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.PoNumber }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.VendorId });
        builder.HasIndex(p => new { p.TenantId, p.Status });

        builder.Ignore(p => p.IsEditable);
        builder.Ignore(p => p.IsCancellable);
        builder.Ignore(p => p.IsReceivable);
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.UomCode).HasMaxLength(20);
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.QuantityReceived).HasColumnType("numeric(18,4)");
        builder.Property(l => l.QuantityBilled).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitCost).HasColumnType("numeric(18,4)");
        builder.Property(l => l.TaxRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.TaxAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineSubtotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineTotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineNotes).HasMaxLength(1000);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.PurchaseOrderId);
        builder.HasIndex(l => l.ProductId);
        builder.Ignore(l => l.QuantityRemainingToReceive);
    }
}
