using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReturnNumber).HasMaxLength(64).IsRequired();
        builder.Property(r => r.CustomerNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Currency).HasMaxLength(3).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(30).HasConversion<string>();
        builder.Property(r => r.Reason).HasMaxLength(40).HasConversion<string>();
        builder.Property(r => r.ReasonText).HasMaxLength(500);
        builder.Property(r => r.RejectionReason).HasMaxLength(500);
        builder.Property(r => r.InternalNotes).HasMaxLength(2000);
        builder.Property(r => r.CustomerNotes).HasMaxLength(2000);

        builder.Property(r => r.RequestedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.ApprovedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.RejectedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.ReceivedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreditNoteIssuedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.RefundedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CancelledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(r => r.Order).WithMany().HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Lines)
            .WithOne(l => l.ReturnRequest)
            .HasForeignKey(l => l.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.ReturnNumber }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.OrderId });
        builder.HasIndex(r => new { r.TenantId, r.CustomerId });
        builder.HasIndex(r => new { r.TenantId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.RequestedAtUtc }).IsDescending(false, true);

        builder.Ignore(r => r.LineSubtotal);
        builder.Ignore(r => r.TaxTotal);
        builder.Ignore(r => r.Total);
        builder.Ignore(r => r.IsTerminal);
    }
}

public class ReturnRequestLineConfiguration : IEntityTypeConfiguration<ReturnRequestLine>
{
    public void Configure(EntityTypeBuilder<ReturnRequestLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.UomCode).HasMaxLength(20);
        builder.Property(l => l.LineNotes).HasMaxLength(500);

        builder.Property(l => l.QuantityReturned).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitCostSnapshot).HasColumnType("numeric(18,4)");
        builder.Property(l => l.TaxRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.TaxAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineSubtotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineTotal).HasColumnType("numeric(18,4)");

        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.OrderLine).WithMany().HasForeignKey(l => l.OrderLineId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.ReturnRequestId);
        builder.HasIndex(l => l.ProductId);
        builder.HasIndex(l => l.OrderLineId);
    }
}
