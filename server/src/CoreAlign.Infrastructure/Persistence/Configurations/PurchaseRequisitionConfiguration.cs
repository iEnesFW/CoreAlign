using CoreAlign.Domain.Entities.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class PurchaseRequisitionConfiguration : IEntityTypeConfiguration<PurchaseRequisition>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisition> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Number).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Status).HasMaxLength(24).HasConversion<string>();
        builder.Property(p => p.Reason).HasMaxLength(24).HasConversion<string>();
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.RejectReason).HasMaxLength(1000);
        builder.Property(p => p.CancelReason).HasMaxLength(1000);
        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(p => p.RequestedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.SubmittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ApprovedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.RejectedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CancelledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ConvertedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.DeletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.Requisition)
            .HasForeignKey(l => l.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.Number }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => new { p.TenantId, p.Status });
        builder.HasIndex(p => new { p.TenantId, p.RequestedAtUtc }).IsDescending(false, true);

        builder.Ignore(p => p.IsEditable);
    }
}

public class PurchaseRequisitionLineConfiguration : IEntityTypeConfiguration<PurchaseRequisitionLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisitionLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.QuantityRequested).HasColumnType("numeric(18,4)");
        builder.Property(l => l.EstimatedUnitCost).HasColumnType("numeric(18,4)");
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.ExpectedDeliveryDate).HasColumnType("timestamp with time zone");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.RequisitionId);
        builder.HasIndex(l => new { l.TenantId, l.ProductId });
        builder.Ignore(l => l.EstimatedLineTotal);
    }
}
