using CoreAlign.Domain.Entities.Manufacturing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Manufacturing;

public class WorkCenterConfiguration : IEntityTypeConfiguration<WorkCenter>
{
    public void Configure(EntityTypeBuilder<WorkCenter> builder)
    {
        builder.HasKey(w => w.Id);
        builder.ToTable("work_centers");

        builder.Property(w => w.Code).HasMaxLength(32).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(128).IsRequired();
        builder.Property(w => w.DailyCapacityMinutes).HasColumnType("numeric(18,4)");
        builder.Property(w => w.IsActive).HasDefaultValue(true);
        builder.Property(w => w.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(w => w.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(w => new { w.TenantId, w.Code })
            .IsUnique()
            .HasDatabaseName("ix_work_centers_tenant_code_unique");
        builder.HasIndex(w => new { w.TenantId, w.IsActive });
    }
}
