using CoreAlign.Domain.Entities.Compliance;
using CoreAlign.Domain.Entities.Treasury;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class EntityAuditLogConfiguration : IEntityTypeConfiguration<EntityAuditLog>
{
    public void Configure(EntityTypeBuilder<EntityAuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityType).HasMaxLength(120).IsRequired();
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(a => a.BeforeJson).HasColumnType("jsonb");
        builder.Property(a => a.AfterJson).HasColumnType("jsonb");
        builder.Property(a => a.RollingHash).HasMaxLength(128).IsRequired();
        builder.Property(a => a.ChangedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId, a.ChangedAtUtc });
        builder.HasIndex(a => new { a.TenantId, a.Sequence }).IsUnique();
    }
}

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Currency).HasMaxLength(8).IsRequired();
        builder.Property(r => r.Source).HasMaxLength(32).IsRequired();
        builder.Property(r => r.RateAgainstTry).HasColumnType("numeric(18,6)");
        builder.Property(r => r.ValidOnDate).HasColumnType("timestamp with time zone");
        builder.Property(r => r.FetchedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(r => new { r.TenantId, r.Currency, r.ValidOnDate }).IsUnique();
    }
}
