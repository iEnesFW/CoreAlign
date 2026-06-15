using CoreAlign.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.TenantId).IsRequired();
        builder.Property(v => v.ParentProductId).IsRequired();
        builder.Property(v => v.Sku).HasMaxLength(64).IsRequired();
        builder.Property(v => v.Barcode).HasMaxLength(64);
        builder.Property(v => v.VariantAttributesJson)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}")
            .IsRequired();
        builder.Property(v => v.PriceOverride).HasColumnType("decimal(18,4)");
        builder.Property(v => v.StockQuantity).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
        builder.Property(v => v.IsActive).HasDefaultValue(true);

        builder.Property(v => v.ConcurrencyToken)
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        builder.Property(v => v.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(v => v.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(v => new { v.TenantId, v.ParentProductId, v.Sku })
            .IsUnique()
            .HasDatabaseName("ux_product_variants_tenant_parent_sku");

        builder.HasIndex(v => new { v.TenantId, v.ParentProductId })
            .HasDatabaseName("ix_product_variants_tenant_parent");

        builder.HasIndex(v => new { v.TenantId, v.IsActive })
            .HasDatabaseName("ix_product_variants_tenant_active");

        builder.HasOne(v => v.Parent)
            .WithMany()
            .HasForeignKey(v => v.ParentProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
