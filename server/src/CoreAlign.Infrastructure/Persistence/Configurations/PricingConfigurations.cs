using CoreAlign.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class PricingDiscountRuleConfiguration : IEntityTypeConfiguration<DiscountRule>
{
    public void Configure(EntityTypeBuilder<DiscountRule> builder)
    {
        builder.ToTable("pricing_discount_rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).HasMaxLength(32).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Scope).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.ValueType).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(r => r.Value).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(r => r.MinQuantity).HasColumnType("numeric(18,4)");
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.ValidFromUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.ValidUntilUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.IsActive });
        builder.HasIndex(r => new { r.TenantId, r.Scope });

        builder.Property(r => r.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);
    }
}

public class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    public void Configure(EntityTypeBuilder<TaxRule> builder)
    {
        builder.ToTable("pricing_tax_rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).HasMaxLength(32).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Scope).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.RegionCode).HasMaxLength(32);
        builder.Property(r => r.ProductClass).HasMaxLength(64);
        builder.Property(r => r.RatePercent).HasColumnType("numeric(6,3)").IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.ValidFromUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.ValidUntilUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.IsActive });
        builder.HasIndex(r => new { r.TenantId, r.Scope });

        builder.Property(r => r.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);
    }
}
