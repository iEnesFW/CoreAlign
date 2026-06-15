using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ProductSubstituteConfiguration : IEntityTypeConfiguration<ProductSubstitute>
{
    public void Configure(EntityTypeBuilder<ProductSubstitute> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ConversionRate).HasColumnType("numeric(12,6)");
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.Property(s => s.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(s => new { s.TenantId, s.ProductId, s.SubstituteProductId }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.ProductId, s.Priority });
    }
}
