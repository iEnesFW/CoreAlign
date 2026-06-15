using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(h => h.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(h => h.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne<User>().WithMany().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.TenantId, h.UserId, h.CreatedAtUtc })
            .IsDescending(false, false, true);
    }
}
