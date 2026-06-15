using CoreAlign.Domain.Entities.Manufacturing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Mrp;

public class PlannedProductionOrderConfiguration : IEntityTypeConfiguration<PlannedProductionOrder>
{
    public void Configure(EntityTypeBuilder<PlannedProductionOrder> builder)
    {
        builder.ToTable("planned_production_orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(o => o.EstimatedUnitCost).HasColumnType("numeric(18,4)");
        builder.Property(o => o.SourcePolicy).HasConversion<string>().HasMaxLength(30);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.DueDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.ReleaseDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.OriginalQuantity).HasColumnType("numeric(18,4)");
        builder.Property(o => o.OriginalDueDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Ignore(o => o.IsQuantityOverridden);
        builder.Ignore(o => o.IsDueDateOverridden);
        builder.Ignore(o => o.IsCompleted);

        // Completion provenance is not persisted: the canonical signal is Status
        // (Released -> Closed) + UpdatedAtUtc. CompletedAtUtc / ProducedWarehouseId
        // are populated only for the in-flight result DTO, so they are mapped out
        // to avoid a schema migration (snapshot stays clean).
        builder.Ignore(o => o.CompletedAtUtc);
        builder.Ignore(o => o.ProducedWarehouseId);

        builder.HasIndex(o => new { o.TenantId, o.SourcePlanRunId });
        builder.HasIndex(o => new { o.TenantId, o.ProductId });
        builder.HasIndex(o => new { o.TenantId, o.SourcePlanRunId, o.PeggingSourceOrderLineId })
            .HasDatabaseName("ix_planned_production_orders_tenant_run_pegging_order_line");
    }
}
