using CoreAlign.Domain.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public sealed class CustomerMergeLogConfiguration : IEntityTypeConfiguration<CustomerMergeLog>
{
    public void Configure(EntityTypeBuilder<CustomerMergeLog> builder)
    {
        builder.ToTable("customer_merge_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.OperationId).IsRequired();
        builder.Property(l => l.SourceCustomerId).IsRequired();
        builder.Property(l => l.TargetCustomerId).IsRequired();
        builder.Property(l => l.InitiatedByUserId);
        builder.Property(l => l.ExecutedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.Notes).HasMaxLength(2000);

        builder.HasIndex(l => new { l.TenantId, l.OperationId })
            .IsUnique()
            .HasDatabaseName("ix_customer_merge_logs_tenant_operation_unique");
        builder.HasIndex(l => new { l.TenantId, l.SourceCustomerId })
            .HasDatabaseName("ix_customer_merge_logs_tenant_source");
        builder.HasIndex(l => new { l.TenantId, l.TargetCustomerId })
            .HasDatabaseName("ix_customer_merge_logs_tenant_target");
    }
}
