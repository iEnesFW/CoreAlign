using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassPlates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        // DbSet-less entity (accessed via _context.Set<StorageLocation>()); explicit plural table name.
        builder.ToTable("storage_locations");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code).HasMaxLength(60).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Warehouse).WithMany().HasForeignKey(l => l.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.ParentLocation).WithMany().HasForeignKey(l => l.ParentLocationId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.TenantId, l.WarehouseId, l.Code }).IsUnique();
        builder.HasIndex(l => l.ParentLocationId);
    }
}
