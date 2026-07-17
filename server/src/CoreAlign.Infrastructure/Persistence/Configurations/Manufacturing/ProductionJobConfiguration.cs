using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Manufacturing;

public class ProductionJobConfiguration : IEntityTypeConfiguration<ProductionJob>
{
    public void Configure(EntityTypeBuilder<ProductionJob> builder)
    {
        builder.ToTable("production_jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.JobNumber).HasMaxLength(30).IsRequired();
        builder.Property(j => j.PlannedQuantity).HasColumnType("numeric(18,4)");
        builder.Property(j => j.CompletedQuantity).HasColumnType("numeric(18,4)");
        builder.Property(j => j.ScrappedQuantity).HasColumnType("numeric(18,4)");
        builder.Property(j => j.UnitOfMeasure).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.RoutingCodeSnapshot).HasMaxLength(40);
        builder.Property(j => j.RoutingNameSnapshot).HasMaxLength(200);
        builder.Property(j => j.CancellationReason).HasMaxLength(500);
        builder.Property(j => j.Notes).HasMaxLength(2000);
        builder.Property(j => j.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);

        builder.Property(j => j.PlannedStartDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.DueDateUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.ReleasedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.StartedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.CancelledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(j => j.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Ignore(j => j.IsTerminal);
        builder.Ignore(j => j.AllRequiredStepsDone);
        builder.Ignore(j => j.CurrentStep);

        builder.HasMany(j => j.Steps)
            .WithOne()
            .HasForeignKey(s => s.ProductionJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(j => j.Logs)
            .WithOne()
            .HasForeignKey(l => l.ProductionJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Product>().WithMany().HasForeignKey(j => j.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlannedProductionOrder>().WithMany()
            .HasForeignKey(j => j.SourcePlannedProductionOrderId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(j => j.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => new { j.TenantId, j.JobNumber }).IsUnique();
        builder.HasIndex(j => new { j.TenantId, j.Status, j.DueDateUtc, j.Id });
        builder.HasIndex(j => new { j.TenantId, j.ProductId });
    }
}
