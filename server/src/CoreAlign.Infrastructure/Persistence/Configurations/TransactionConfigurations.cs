using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class CustomerTransactionConfiguration : IEntityTypeConfiguration<CustomerTransaction>
{
    public void Configure(EntityTypeBuilder<CustomerTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.Type).HasMaxLength(20).HasConversion<string>();
        builder.Property(t => t.Amount).HasColumnType("numeric(18,4)");
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.Reference).HasMaxLength(64);
        builder.Property(t => t.Notes).HasMaxLength(500);

        builder.HasOne(t => t.Customer).WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.Invoice).WithMany().HasForeignKey(t => t.InvoiceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.Order).WithMany().HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new { t.TenantId, t.CustomerId, t.OccurredAtUtc }).IsDescending(false, false, true);
        builder.HasIndex(t => t.InvoiceId);
    }
}

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.Type).HasMaxLength(20).HasConversion<string>();
        builder.Property(t => t.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(t => t.BalanceAfter).HasColumnType("numeric(18,4)");
        builder.Property(t => t.Reference).HasMaxLength(64);
        builder.Property(t => t.Notes).HasMaxLength(500);

        builder.HasOne(t => t.Product).WithMany().HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.Order).WithMany().HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new { t.TenantId, t.ProductId, t.OccurredAtUtc }).IsDescending(false, false, true);
    }
}
