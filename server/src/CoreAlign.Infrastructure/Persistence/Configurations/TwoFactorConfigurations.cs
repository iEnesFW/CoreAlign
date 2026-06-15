using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class TwoFactorBackupCodeConfiguration : IEntityTypeConfiguration<TwoFactorBackupCode>
{
    public void Configure(EntityTypeBuilder<TwoFactorBackupCode> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(b => b.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UsedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.TenantId, b.UserId });
        builder.HasIndex(b => new { b.TenantId, b.UserId, b.CodeHash }).IsUnique();
    }
}

public class TwoFactorChallengeConfiguration : IEntityTypeConfiguration<TwoFactorChallenge>
{
    public void Configure(EntityTypeBuilder<TwoFactorChallenge> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(c => c.IpAddress).HasMaxLength(45);
        builder.Property(c => c.UserAgent).HasMaxLength(1024);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.ConsumedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.TokenHash).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.UserId });
    }
}
