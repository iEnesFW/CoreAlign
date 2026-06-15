using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).HasMaxLength(64).IsRequired();
        builder.Property(m => m.PayloadJson).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.Result).HasMaxLength(64);
        builder.Property(m => m.LastError).HasMaxLength(2000);
        builder.Property(m => m.ProcessedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        // Drain query: pending rows for a tenant, oldest first.
        builder.HasIndex(m => new { m.TenantId, m.Status, m.CreatedAtUtc });
    }
}
