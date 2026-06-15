using CoreAlign.Domain.Entities.Mrp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Mrp;

public class MrpPlanRunConfiguration : IEntityTypeConfiguration<MrpPlanRun>
{
    public void Configure(EntityTypeBuilder<MrpPlanRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.ToTable("mrp_plan_runs");
        builder.Property(r => r.Number).HasMaxLength(32).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.BucketKind).HasConversion<string>().HasMaxLength(10);
        builder.Property(r => r.IdempotencyKey).HasMaxLength(64).IsRequired();
        builder.Property(r => r.AsOfDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CommittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(r => r.ConcurrencyToken)
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        builder.HasMany(r => r.PlannedOrders)
            .WithOne(o => o.PlanRun)
            .HasForeignKey(o => o.PlanRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.ActionMessages)
            .WithOne(m => m.PlanRun)
            .HasForeignKey(m => m.PlanRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Peggings)
            .WithOne(p => p.PlanRun)
            .HasForeignKey(p => p.PlanRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ix_mrp_plan_runs_tenant_idempotency_unique");
        builder.HasIndex(r => new { r.TenantId, r.AsOfDateUtc });
    }
}
