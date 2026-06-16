using CoreAlign.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class NotificationRateCounterConfiguration : IEntityTypeConfiguration<NotificationRateCounter>
{
    public void Configure(EntityTypeBuilder<NotificationRateCounter> builder)
    {
        builder.ToTable("notification_rate_counters");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ProviderName).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Scope).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(c => c.ScopeKey).HasMaxLength(320).IsRequired();
        builder.Property(c => c.WindowStartUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(c => new { c.TenantId, c.ProviderName, c.Scope, c.ScopeKey, c.WindowStartUtc }).IsUnique();
        builder.HasIndex(c => c.WindowStartUtc);
    }
}
