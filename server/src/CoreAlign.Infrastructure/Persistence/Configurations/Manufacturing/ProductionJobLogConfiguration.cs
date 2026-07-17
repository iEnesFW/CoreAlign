using CoreAlign.Domain.Entities.Manufacturing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Manufacturing;

public class ProductionJobLogConfiguration : IEntityTypeConfiguration<ProductionJobLog>
{
    public void Configure(EntityTypeBuilder<ProductionJobLog> builder)
    {
        builder.ToTable("production_job_logs");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.ProductionJobId).IsRequired();
        builder.Property(x => x.ProductionJobStepId).IsRequired();
        builder.Property(x => x.OperatorId).IsRequired();
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.EventTimeUtc).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.DurationMinutes);

        builder.HasOne<ProductionJobStep>()
            .WithMany()
            .HasForeignKey(x => x.ProductionJobStepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
