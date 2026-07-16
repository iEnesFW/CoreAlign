using CoreAlign.Domain.Entities.Manufacturing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Manufacturing;

public class ProductionJobStepConfiguration : IEntityTypeConfiguration<ProductionJobStep>
{
    public void Configure(EntityTypeBuilder<ProductionJobStep> builder)
    {
        builder.ToTable("production_job_steps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OperationName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.OperationType).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.SetupTimeMinutes).HasColumnType("numeric(18,4)");
        builder.Property(s => s.RunTimeMinutesPerUnit).HasColumnType("numeric(18,4)");
        builder.Property(s => s.RunTimeMinutesPerSqm).HasColumnType("numeric(18,4)");
        builder.Property(s => s.ScrapPercentage).HasColumnType("numeric(6,3)");
        builder.Property(s => s.InputQuantity).HasColumnType("numeric(18,4)");
        builder.Property(s => s.ActualSetupMinutes).HasColumnType("numeric(18,4)");
        builder.Property(s => s.ActualRunMinutes).HasColumnType("numeric(18,4)");
        builder.Property(s => s.GoodQuantity).HasColumnType("numeric(18,4)");
        builder.Property(s => s.ScrappedQuantity).HasColumnType("numeric(18,4)");
        builder.Property(s => s.Instructions).HasMaxLength(2000);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.StartedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.FinishedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne<WorkCenter>().WithMany()
            .HasForeignKey(s => s.WorkCenterId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.TenantId, s.ProductionJobId, s.StepNumber }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.WorkCenterId });
        builder.HasIndex(s => new { s.TenantId, s.Status });
    }
}
