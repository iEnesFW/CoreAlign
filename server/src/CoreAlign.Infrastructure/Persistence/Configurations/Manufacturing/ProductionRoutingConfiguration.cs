using CoreAlign.Domain.Entities.Manufacturing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Manufacturing;

public class ProductionRoutingConfiguration : IEntityTypeConfiguration<ProductionRouting>
{
    public void Configure(EntityTypeBuilder<ProductionRouting> builder)
    {
        builder.ToTable("production_routings");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).HasMaxLength(40).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RoutingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.Status });
    }
}
