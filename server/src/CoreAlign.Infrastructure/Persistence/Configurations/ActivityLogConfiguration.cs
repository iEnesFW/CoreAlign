using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Method).HasMaxLength(8).IsRequired();
        builder.Property(l => l.Path).HasMaxLength(512).IsRequired();
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasMaxLength(1024);
        builder.Property(l => l.TraceId).HasMaxLength(128);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(l => new { l.TenantId, l.CreatedAtUtc }).IsDescending(false, true);
        builder.HasIndex(l => new { l.TenantId, l.UserId, l.CreatedAtUtc }).IsDescending(false, false, true);
    }
}
