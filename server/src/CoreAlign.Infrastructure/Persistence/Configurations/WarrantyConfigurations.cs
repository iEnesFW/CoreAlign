using CoreAlign.Domain.Entities.Warranty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class WarrantyContractConfiguration : IEntityTypeConfiguration<WarrantyContract>
{
    public void Configure(EntityTypeBuilder<WarrantyContract> builder)
    {
        builder.ToTable("warranty_contracts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Number).HasMaxLength(32).IsRequired();
        builder.Property(c => c.CoverageType).HasConversion<string>().HasMaxLength(32);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(c => c.TermsJson).HasMaxLength(32000).IsRequired();
        builder.Property(c => c.Notes).HasMaxLength(4000);
        builder.Property(c => c.CancellationReason).HasMaxLength(1000);

        builder.Property(c => c.StartDate).HasColumnType("timestamp with time zone");
        builder.Property(c => c.EndDate).HasColumnType("timestamp with time zone");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(c => c.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(c => new { c.TenantId, c.Number })
            .HasDatabaseName("ux_warranty_contracts_tenant_number")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(c => new { c.TenantId, c.CustomerId })
            .HasDatabaseName("ix_warranty_contracts_tenant_customer");

        builder.HasIndex(c => new { c.TenantId, c.OrderId })
            .HasDatabaseName("ix_warranty_contracts_tenant_order");

        builder.HasIndex(c => new { c.TenantId, c.Status })
            .HasDatabaseName("ix_warranty_contracts_tenant_status");

        builder.HasIndex(c => new { c.TenantId, c.EndDate })
            .HasDatabaseName("ix_warranty_contracts_tenant_end_date");
    }
}

public class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
{
    public void Configure(EntityTypeBuilder<MaintenanceSchedule> builder)
    {
        builder.ToTable("maintenance_schedules");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(24);
        builder.Property(s => s.RecurrencePattern).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.NextDueDate).HasColumnType("timestamp with time zone");
        builder.Property(s => s.LastCompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(s => new { s.TenantId, s.WarrantyContractId })
            .HasDatabaseName("ix_maintenance_schedules_tenant_contract");

        builder.HasIndex(s => new { s.TenantId, s.NextDueDate })
            .HasDatabaseName("ix_maintenance_schedules_tenant_next_due");
    }
}

public class ServiceTicketConfiguration : IEntityTypeConfiguration<ServiceTicket>
{
    public void Configure(EntityTypeBuilder<ServiceTicket> builder)
    {
        builder.ToTable("service_tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.DescriptionMd).HasMaxLength(8000).IsRequired();
        builder.Property(t => t.ResolutionNotesMd).HasMaxLength(8000);
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.ChargeableAmount).HasPrecision(18, 4);

        builder.Property(t => t.ReportedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.ResolvedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(t => t.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(t => new { t.TenantId, t.CustomerId })
            .HasDatabaseName("ix_service_tickets_tenant_customer");

        builder.HasIndex(t => new { t.TenantId, t.Status })
            .HasDatabaseName("ix_service_tickets_tenant_status");

        builder.HasIndex(t => new { t.TenantId, t.Priority })
            .HasDatabaseName("ix_service_tickets_tenant_priority");

        builder.HasIndex(t => new { t.TenantId, t.WarrantyContractId })
            .HasDatabaseName("ix_service_tickets_tenant_warranty");
    }
}
