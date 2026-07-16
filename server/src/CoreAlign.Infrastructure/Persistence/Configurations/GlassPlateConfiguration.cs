using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassPlates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class GlassPlateConfiguration : IEntityTypeConfiguration<GlassPlate>
{
    public void Configure(EntityTypeBuilder<GlassPlate> builder)
    {
        // DbSet-less entity (accessed via _context.Set<GlassPlate>()); explicit plural table name.
        builder.ToTable("glass_plates");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PlateNumber).HasMaxLength(60).IsRequired();
        builder.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Condition).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.WidthMm).HasColumnType("numeric(10,2)");
        builder.Property(p => p.HeightMm).HasColumnType("numeric(10,2)");
        builder.Property(p => p.ThicknessMm).HasColumnType("numeric(9,2)");
        builder.Property(p => p.OriginalAreaMm2).HasColumnType("numeric(18,4)");
        builder.Property(p => p.RemainingAreaMm2).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.ReceivedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ConsumedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);

        builder.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Warehouse).WithMany().HasForeignKey(p => p.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.StorageLocation).WithMany().HasForeignKey(p => p.StorageLocationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Lot>().WithMany().HasForeignKey(p => p.LotId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.TenantId, p.PlateNumber }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.ProductId, p.Status });
        builder.HasIndex(p => new { p.TenantId, p.WarehouseId, p.StorageLocationId });
        builder.HasIndex(p => p.ParentPlateId);
        builder.HasIndex(p => new { p.TenantId, p.ProductId, p.RemainingAreaMm2 }).HasFilter("status = 'Available'");
    }
}
