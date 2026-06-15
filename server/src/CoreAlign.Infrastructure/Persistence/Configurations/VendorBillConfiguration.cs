using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class VendorBillConfiguration : IEntityTypeConfiguration<VendorBill>
{
    public void Configure(EntityTypeBuilder<VendorBill> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.VendorName).HasMaxLength(300).IsRequired();
        builder.Property(b => b.BillNumber).HasMaxLength(64).IsRequired();
        builder.Property(b => b.Status).HasMaxLength(24).HasConversion<string>();
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired();
        builder.Property(b => b.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(b => b.Subtotal).HasColumnType("numeric(18,4)");
        builder.Property(b => b.TaxAmount).HasColumnType("numeric(18,4)");
        builder.Property(b => b.Total).HasColumnType("numeric(18,4)");
        builder.Property(b => b.AmountPaid).HasColumnType("numeric(18,4)");
        builder.Property(b => b.Notes).HasMaxLength(2000);
        builder.Property(b => b.BillDate).HasColumnType("timestamp with time zone");
        builder.Property(b => b.DueDate).HasColumnType("timestamp with time zone");
        builder.Property(b => b.PostedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(b => b.Vendor).WithMany().HasForeignKey(b => b.VendorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.TenantId, b.VendorId, b.BillNumber }).IsUnique();
        builder.HasIndex(b => new { b.TenantId, b.Status });
        builder.HasIndex(b => new { b.TenantId, b.BillDate }).IsDescending(false, true);

        builder.Ignore(b => b.AmountDue);
    }
}

public class VendorPaymentConfiguration : IEntityTypeConfiguration<VendorPayment>
{
    public void Configure(EntityTypeBuilder<VendorPayment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.VendorName).HasMaxLength(300).IsRequired();
        builder.Property(p => p.PaymentNumber).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(p => p.Amount).HasColumnType("numeric(18,4)");
        builder.Property(p => p.AppliedAmount).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Method).HasMaxLength(40);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.VoidReason).HasMaxLength(500);
        builder.Property(p => p.PaymentDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.VoidedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(p => p.Vendor).WithMany().HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.TenantId, p.PaymentNumber }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.VendorId });
        builder.HasIndex(p => new { p.TenantId, p.VendorBillId });

        builder.Ignore(p => p.UnappliedAmount);
        builder.Ignore(p => p.IsDraft);
    }
}

public class VendorPaymentApplicationConfiguration : IEntityTypeConfiguration<VendorPaymentApplication>
{
    public void Configure(EntityTypeBuilder<VendorPaymentApplication> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AppliedAmount).HasColumnType("numeric(18,4)");
        builder.Property(a => a.AppliedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(a => a.VendorPayment).WithMany().HasForeignKey(a => a.VendorPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.VendorBill).WithMany().HasForeignKey(a => a.VendorBillId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.VendorPaymentId });
        builder.HasIndex(a => new { a.TenantId, a.VendorBillId });
    }
}

public class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CountNumber).HasMaxLength(64).IsRequired();
        builder.Property(c => c.WarehouseCode).HasMaxLength(32).IsRequired();
        builder.Property(c => c.WarehouseName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.PlannedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.CountingStartedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.ReconciledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.PostedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(c => c.Warehouse).WithMany().HasForeignKey(c => c.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(c => c.Lines).WithOne(l => l.StockCount).HasForeignKey(l => l.StockCountId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TenantId, c.CountNumber }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.WarehouseId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.PlannedAtUtc }).IsDescending(false, true);

        builder.Ignore(c => c.TotalVarianceQuantity);
        builder.Ignore(c => c.TotalVarianceCost);
    }
}

public class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.LotNumber).HasMaxLength(64);
        builder.Property(l => l.BinLocation).HasMaxLength(64);
        builder.Property(l => l.LineNotes).HasMaxLength(500);
        builder.Property(l => l.ExpectedQuantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.CountedQuantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.VarianceQuantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.SnapshotUnitCost).HasColumnType("numeric(18,4)");
        builder.Property(l => l.VarianceCost).HasColumnType("numeric(18,4)");
        builder.Property(l => l.CountedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Lot).WithMany().HasForeignKey(l => l.LotId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.StockCountId);
        builder.HasIndex(l => l.ProductId);

        builder.Ignore(l => l.IsCounted);
    }
}
