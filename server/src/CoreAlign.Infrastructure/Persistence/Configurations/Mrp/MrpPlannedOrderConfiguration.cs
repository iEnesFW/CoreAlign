using CoreAlign.Domain.Entities.Mrp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Mrp;

public class MrpPlannedOrderConfiguration : IEntityTypeConfiguration<MrpPlannedOrder>
{
    public void Configure(EntityTypeBuilder<MrpPlannedOrder> builder)
    {
        builder.HasKey(o => o.Id);
        builder.ToTable("mrp_planned_orders");
        builder.Property(o => o.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(o => o.EstimatedUnitCost).HasColumnType("numeric(18,4)");
        builder.Property(o => o.SourcePolicy).HasConversion<string>().HasMaxLength(30);
        builder.Property(o => o.DueDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.ReleaseDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.OriginalQuantity).HasColumnType("numeric(18,4)");
        builder.Property(o => o.OriginalDueDateUtc).HasColumnType("timestamp with time zone");
        builder.Ignore(o => o.IsQuantityOverridden);
        builder.Ignore(o => o.IsDueDateOverridden);
        builder.Property(o => o.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(o => new { o.TenantId, o.PlanRunId });
        builder.HasIndex(o => new { o.TenantId, o.ProductId });
    }
}
