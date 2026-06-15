using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ModeOverride).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.PerScreenOverridesJson).HasMaxLength(2000);
        builder.Property(p => p.LocaleOverride).HasMaxLength(16);
        builder.Property(p => p.ThemeOverride).HasMaxLength(16);
        builder.Property(p => p.DeletedReason).HasMaxLength(500);
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.DeletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
