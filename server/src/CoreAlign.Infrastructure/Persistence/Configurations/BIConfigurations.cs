using CoreAlign.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public sealed class DashboardWidgetConfiguration : IEntityTypeConfiguration<DashboardWidget>
{
    public void Configure(EntityTypeBuilder<DashboardWidget> builder)
    {
        builder.ToTable("dashboard_widgets");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Title).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(w => w.DataSource).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(w => w.QueryConfigJson).HasColumnType("text").IsRequired();
        builder.Property(w => w.GridX).IsRequired();
        builder.Property(w => w.GridY).IsRequired();
        builder.Property(w => w.Width).IsRequired();
        builder.Property(w => w.Height).IsRequired();
        builder.Property(w => w.DisplayOrder).IsRequired();
        builder.Property(w => w.IsActive).IsRequired();
        builder.Property(w => w.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(w => w.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(w => new { w.TenantId, w.UserId, w.IsActive })
            .HasDatabaseName("ix_dashboard_widgets_tenant_user_active");
    }
}

public sealed class SavedReportConfiguration : IEntityTypeConfiguration<SavedReport>
{
    public void Configure(EntityTypeBuilder<SavedReport> builder)
    {
        builder.ToTable("saved_reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.DataSource).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.QueryConfigJson).HasColumnType("text").IsRequired();
        builder.Property(r => r.IsPublic).IsRequired();
        builder.Property(r => r.OwnerUserId).IsRequired();
        builder.Property(r => r.LastRunAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.LastRunRowCount);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(r => new { r.TenantId, r.OwnerUserId })
            .HasDatabaseName("ix_saved_reports_tenant_owner");
        builder.HasIndex(r => new { r.TenantId, r.IsPublic })
            .HasDatabaseName("ix_saved_reports_tenant_public");
    }
}

public sealed class ReportRunConfiguration : IEntityTypeConfiguration<ReportRun>
{
    public void Configure(EntityTypeBuilder<ReportRun> builder)
    {
        builder.ToTable("report_runs");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SavedReportId).IsRequired();
        builder.Property(r => r.RanByUserId).IsRequired();
        builder.Property(r => r.RanAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(r => r.ResultRowCount).IsRequired();
        builder.Property(r => r.ExportFormat).HasConversion<string>().HasMaxLength(10);
        builder.Property(r => r.DurationMs);
        builder.Property(r => r.ErrorMessage).HasMaxLength(2000);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(r => new { r.TenantId, r.SavedReportId, r.RanAtUtc })
            .HasDatabaseName("ix_report_runs_tenant_saved_at");
    }
}
