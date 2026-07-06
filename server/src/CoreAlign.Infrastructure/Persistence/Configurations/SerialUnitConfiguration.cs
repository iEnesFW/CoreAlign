using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class SerialUnitConfiguration : IEntityTypeConfiguration<SerialUnit>
{
    public void Configure(EntityTypeBuilder<SerialUnit> builder)
    {
        // DbSet-less entity (accessed via _context.Set<SerialUnit>()); explicit plural table name.
        builder.ToTable("stock_serial_units");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SerialNumber).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.UnitCost).HasColumnType("numeric(18,4)");
        builder.Property(s => s.ReceivedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);

        // Serial identity is tenant + product + serial number (a serial string can repeat across
        // products / tenants).
        builder.HasIndex(s => new { s.TenantId, s.ProductId, s.SerialNumber }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.SerialNumber });
        builder.HasIndex(s => s.ParentSerialUnitId);
    }
}
