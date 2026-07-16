using CoreAlign.Domain.Entities.Manufacturing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Manufacturing;

public class RoutingStepConfiguration : IEntityTypeConfiguration<RoutingStep>
{
    public void Configure(EntityTypeBuilder<RoutingStep> builder)
    {
        builder.ToTable("routing_steps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OperationName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.OperationType).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.SetupTimeMinutes).HasColumnType("numeric(18,4)");
        builder.Property(s => s.RunTimeMinutesPerUnit).HasColumnType("numeric(18,4)");
        builder.Property(s => s.RunTimeMinutesPerSqm).HasColumnType("numeric(18,4)");
        builder.Property(s => s.ScrapPercentage).HasColumnType("numeric(6,3)");
        builder.Property(s => s.Instructions).HasMaxLength(2000);
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne<WorkCenter>()
            .WithMany()
            .HasForeignKey(s => s.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.TenantId, s.RoutingId, s.StepNumber }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.WorkCenterId });
    }
}
