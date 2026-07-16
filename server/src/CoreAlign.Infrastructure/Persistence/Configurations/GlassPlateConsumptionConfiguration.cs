using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassPlates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class GlassPlateConsumptionConfiguration : IEntityTypeConfiguration<GlassPlateConsumption>
{
    public void Configure(EntityTypeBuilder<GlassPlateConsumption> builder)
    {
        // DbSet-less entity (accessed via _context.Set<GlassPlateConsumption>()); explicit plural table name.
        builder.ToTable("glass_plate_consumptions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CutAreaMm2).HasColumnType("numeric(18,4)");
        builder.Property(c => c.ScrappedAreaMm2).HasColumnType("numeric(18,4)");
        builder.Property(c => c.CutWidthMm).HasColumnType("numeric(10,2)");
        builder.Property(c => c.CutHeightMm).HasColumnType("numeric(10,2)");
        builder.Property(c => c.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(c => c.GlassPlate).WithMany().HasForeignKey(c => c.GlassPlateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(c => c.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(c => c.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockReasonCode>().WithMany().HasForeignKey(c => c.ScrapReasonCodeId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.TenantId, c.GlassPlateId });
        builder.HasIndex(c => new { c.TenantId, c.OrderLineId });
        builder.HasIndex(c => new { c.TenantId, c.ProductId, c.WarehouseId });
    }
}
