using CoreAlign.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("report_definitions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.EntityType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(r => r.DimensionsJson).HasColumnType("text").IsRequired();
        builder.Property(r => r.MeasuresJson).HasColumnType("text").IsRequired();
        builder.Property(r => r.FiltersJson).HasColumnType("text").IsRequired();
        builder.Property(r => r.SortByJson).HasColumnType("text");
        builder.Property(r => r.Limit);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(r => new { r.TenantId, r.Name })
            .IsUnique()
            .HasDatabaseName("ix_report_definitions_tenant_name_unique");
        builder.HasIndex(r => new { r.TenantId, r.EntityType });
    }
}

public sealed class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.ToTable("report_schedules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.ReportKey).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Frequency).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Format).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(r => r.CronExpression).HasMaxLength(100);
        builder.Property(r => r.RecipientsJson).HasColumnType("text").IsRequired();
        builder.Property(r => r.FiltersJson).HasColumnType("text").IsRequired();
        builder.Property(r => r.NextRunAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(r => r.LastRunAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.LastRunStatus).HasMaxLength(40);
        builder.Property(r => r.LastRunError).HasMaxLength(2000);
        builder.Property(r => r.IsActive).IsRequired();
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(r => new { r.TenantId, r.IsActive, r.NextRunAtUtc })
            .HasDatabaseName("ix_report_schedules_tenant_due");
        builder.HasIndex(r => r.NextRunAtUtc)
            .HasDatabaseName("ix_report_schedules_due");
    }
}
