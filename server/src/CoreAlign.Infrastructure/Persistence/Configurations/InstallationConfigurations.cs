using CoreAlign.Domain.Entities.Installation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class InstallationAcceptanceConfiguration : IEntityTypeConfiguration<InstallationAcceptance>
{
    public void Configure(EntityTypeBuilder<InstallationAcceptance> builder)
    {
        builder.ToTable("installation_acceptances");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(a => a.ChecklistJson).HasMaxLength(32000).IsRequired();
        builder.Property(a => a.PhotoFileIds).HasMaxLength(8000).IsRequired();
        builder.Property(a => a.NotesMd).HasMaxLength(8000);
        builder.Property(a => a.RejectionReason).HasMaxLength(2000);
        builder.Property(a => a.CustomerName).HasMaxLength(200);
        builder.Property(a => a.AcceptIdempotencyKey).HasMaxLength(128);

        builder.Property(a => a.StartedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.CustomerSignatureCapturedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(a => a.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(a => new { a.TenantId, a.WorkOrderId })
            .HasDatabaseName("ux_installation_acceptances_tenant_workorder")
            .IsUnique();

        builder.HasIndex(a => new { a.TenantId, a.ProjectId })
            .HasDatabaseName("ix_installation_acceptances_tenant_project");

        builder.HasIndex(a => new { a.TenantId, a.CustomerId })
            .HasDatabaseName("ix_installation_acceptances_tenant_customer");

        builder.HasIndex(a => new { a.TenantId, a.InspectorUserId })
            .HasDatabaseName("ix_installation_acceptances_tenant_inspector");

        builder.HasIndex(a => new { a.TenantId, a.Status })
            .HasDatabaseName("ix_installation_acceptances_tenant_status");

        builder.HasIndex(a => new { a.TenantId, a.AcceptIdempotencyKey })
            .HasDatabaseName("ux_installation_acceptances_tenant_accept_idempotency_key")
            .IsUnique()
            .HasFilter("\"accept_idempotency_key\" IS NOT NULL");
    }
}

public class PunchListItemConfiguration : IEntityTypeConfiguration<PunchListItem>
{
    public void Configure(EntityTypeBuilder<PunchListItem> builder)
    {
        builder.ToTable("punch_list_items");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.Severity).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.ResolutionNotes).HasMaxLength(4000);

        builder.Property(p => p.ResolvedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.AcceptanceId })
            .HasDatabaseName("ix_punch_list_items_tenant_acceptance");

        builder.HasIndex(p => new { p.TenantId, p.Status })
            .HasDatabaseName("ix_punch_list_items_tenant_status");
    }
}
