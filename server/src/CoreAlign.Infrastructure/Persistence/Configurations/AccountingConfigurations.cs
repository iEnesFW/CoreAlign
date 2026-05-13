using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(10).IsRequired();
        builder.Property(p => p.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.StartDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.EndDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ClosedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ReopenedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.Year, p.Month }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.Status });

        builder.Ignore(p => p.IsClosed);
    }
}

public class CustomerProductPriceConfiguration : IEntityTypeConfiguration<CustomerProductPrice>
{
    public void Configure(EntityTypeBuilder<CustomerProductPrice> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Price).HasColumnType("numeric(18,4)");
        builder.Property(p => p.DiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(p => p.MinQuantity).HasColumnType("numeric(18,4)");
        builder.Property(p => p.MaxQuantity).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.ValidFromUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ValidUntilUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.CustomerId, p.ProductId });
        builder.HasIndex(p => new { p.TenantId, p.ProductId, p.IsActive });
    }
}
