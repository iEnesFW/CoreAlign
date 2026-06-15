using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Purpose).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Version).HasMaxLength(32).IsRequired();
        builder.Property(c => c.AnonymousFingerprint).HasMaxLength(64);
        builder.Property(c => c.IpAddress).HasMaxLength(45);
        builder.Property(c => c.UserAgent).HasMaxLength(256);
        builder.Property(c => c.CapturedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.WithdrawnAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(c => new { c.TenantId, c.UserId, c.Purpose, c.CapturedAtUtc })
            .IsDescending(false, false, false, true);

        builder.HasIndex(c => new { c.TenantId, c.AnonymousFingerprint, c.Purpose })
            .HasFilter(null);
    }
}
