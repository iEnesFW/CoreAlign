using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations.Manufacturing;

public class WorkCenterOperatorConfiguration : IEntityTypeConfiguration<WorkCenterOperator>
{
    public void Configure(EntityTypeBuilder<WorkCenterOperator> builder)
    {
        builder.ToTable("work_center_operators");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.QualificationLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.CertifiedOn).HasColumnType("date");
        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.Property(o => o.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne<WorkCenter>()
            .WithMany()
            .HasForeignKey(o => o.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.TenantId, o.WorkCenterId, o.EmployeeId })
            .IsUnique()
            .HasFilter("is_active = true");
        builder.HasIndex(o => new { o.TenantId, o.WorkCenterId })
            .IsUnique()
            .HasFilter("is_primary = true AND is_active = true");
        builder.HasIndex(o => new { o.TenantId, o.EmployeeId });
    }
}
