using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ProductComponentConfiguration : IEntityTypeConfiguration<ProductComponent>
{
    public void Configure(EntityTypeBuilder<ProductComponent> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(c => c.ParentProduct)
            .WithMany()
            .HasForeignKey(c => c.ParentProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ComponentProduct)
            .WithMany()
            .HasForeignKey(c => c.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.ParentProductId });
        builder.HasIndex(c => new { c.TenantId, c.ParentProductId, c.ComponentProductId })
            .IsUnique()
            .HasDatabaseName("ix_product_components_parent_component_unique");
    }
}
