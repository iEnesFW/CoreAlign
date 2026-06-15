using CoreAlign.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductId).IsRequired();
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(p => p.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(p => p.SizeBytes).IsRequired();
        builder.Property(p => p.AltText).HasMaxLength(256);
        builder.Property(p => p.DisplayOrder).HasDefaultValue(0);
        builder.Property(p => p.IsPrimary).HasDefaultValue(false);

        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UploadedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.ProductId, p.DisplayOrder });
        builder.HasIndex(p => new { p.TenantId, p.ProductId })
            .IsUnique()
            .HasFilter("is_primary = true")
            .HasDatabaseName("ux_product_images_tenant_product_primary");

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
