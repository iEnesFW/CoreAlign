using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Privacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class DataSubjectRequestConfiguration : IEntityTypeConfiguration<DataSubjectRequest>
{
    public void Configure(EntityTypeBuilder<DataSubjectRequest> builder)
    {
        builder.ToTable("data_subject_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestType).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>().HasDefaultValue(DataSubjectRequestStatus.Submitted);
        builder.Property(r => r.LegalBasisOverride).HasConversion<int>().HasDefaultValue(Domain.Entities.LegalBasisOverride.None);

        builder.Property(r => r.UsernameHash).HasMaxLength(128);
        builder.Property(r => r.EmailHash).HasMaxLength(128);
        builder.Property(r => r.RejectionReason).HasMaxLength(2000);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.DeletedReason).HasMaxLength(500);

        builder.Property(r => r.RequestedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.SubmittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(r => r.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(r => new { r.TenantId, r.Status })
            .HasDatabaseName("ix_data_subject_requests_tenant_status");
        builder.HasIndex(r => new { r.TenantId, r.RequesterCustomerId })
            .HasDatabaseName("ix_data_subject_requests_tenant_customer");
    }
}

public class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("retention_policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(p => p.RetentionDays).IsRequired();
        builder.Property(p => p.ActionOnExpiry).HasConversion<int>();
        builder.Property(p => p.DeletedReason).HasMaxLength(500);

        builder.Property(p => p.LastRunAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(p => new { p.TenantId, p.EntityType })
            .IsUnique()
            .HasDatabaseName("ux_retention_policies_tenant_entity")
            .HasFilter("is_deleted = false");
    }
}
