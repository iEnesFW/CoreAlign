using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Sku).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(64);
        builder.Property(p => p.Mpn).HasMaxLength(64);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ShortDescription).HasMaxLength(500);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Slug).HasMaxLength(200);
        builder.Property(p => p.Unit).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.VariantAttributesJson).HasColumnType("jsonb");
        builder.Property(p => p.Color).HasMaxLength(60);
        builder.Property(p => p.ThicknessMm).HasColumnType("numeric(9,2)");
        builder.HasIndex(p => new { p.TenantId, p.Color });
        builder.HasIndex(p => new { p.TenantId, p.ThicknessMm });
        builder.Property(p => p.TagsJson).HasColumnType("jsonb");

        builder.Property(p => p.Price).HasColumnType("numeric(18,4)");
        builder.Property(p => p.ListPrice).HasColumnType("numeric(18,4)");
        builder.Property(p => p.MinSellingPrice).HasColumnType("numeric(18,4)");
        builder.Property(p => p.StandardCost).HasColumnType("numeric(18,4)");
        builder.Property(p => p.LastPurchaseCost).HasColumnType("numeric(18,4)");
        builder.Property(p => p.AverageCost).HasColumnType("numeric(18,4)");

        builder.Property(p => p.StockQuantity).HasColumnType("numeric(18,4)");
        builder.Property(p => p.MinStock).HasColumnType("numeric(18,4)");
        builder.Property(p => p.MaxStock).HasColumnType("numeric(18,4)");
        builder.Property(p => p.ReorderPoint).HasColumnType("numeric(18,4)");
        builder.Property(p => p.SafetyStock).HasColumnType("numeric(18,4)");

        builder.Property(p => p.WeightKg).HasColumnType("numeric(18,4)");
        builder.Property(p => p.WidthCm).HasColumnType("numeric(18,4)");
        builder.Property(p => p.HeightCm).HasColumnType("numeric(18,4)");
        builder.Property(p => p.DepthCm).HasColumnType("numeric(18,4)");
        builder.Property(p => p.VolumeM3).HasColumnType("numeric(18,6)");

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);
        builder.Property(p => p.ProcurementType).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.CostingMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.AbcClass).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.WorkCenterId).HasColumnName("work_center_id");
        builder.Property(p => p.RunTimeMinutesPerUnit).HasColumnName("run_time_minutes_per_unit").HasColumnType("numeric(18,4)");
        builder.Property(p => p.LaunchDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.EndOfLifeDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(p => p.Brand).WithMany().HasForeignKey(p => p.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.TaxRate).WithMany().HasForeignKey(p => p.TaxRateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.BaseUom).WithMany().HasForeignKey(p => p.BaseUomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.ParentProduct).WithMany().HasForeignKey(p => p.ParentProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.Barcode })
            .IsUnique()
            .HasFilter("barcode IS NOT NULL")
            .HasDatabaseName("ix_products_tenant_barcode_unique");
        builder.HasIndex(p => new { p.TenantId, p.Name });
        builder.HasIndex(p => new { p.TenantId, p.Status });
        builder.HasIndex(p => new { p.TenantId, p.CategoryId });
        builder.HasIndex(p => new { p.TenantId, p.BrandId });

        builder.Ignore(p => p.IsActive);
    }
}
