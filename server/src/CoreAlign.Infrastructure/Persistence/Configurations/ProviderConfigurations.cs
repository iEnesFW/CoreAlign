using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class TenantProviderConfigConfiguration : IEntityTypeConfiguration<TenantProviderConfig>
{
    public void Configure(EntityTypeBuilder<TenantProviderConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(c => c.ProviderName).HasMaxLength(64).IsRequired();
        builder.Property(c => c.DisplayName).HasMaxLength(200);
        builder.Property(c => c.LastHealthStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.LastHealthMessage).HasMaxLength(2000);
        builder.Property(c => c.EncryptedCredentialsJson).HasMaxLength(8000);
        builder.Property(c => c.LastHealthCheckUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(c => new { c.TenantId, c.Category, c.ProviderName })
            .IsUnique()
            .HasDatabaseName("ix_tenant_provider_configs_tenant_category_provider");

        builder.HasIndex(c => new { c.TenantId, c.Category, c.IsDefault })
            .HasFilter("is_default = true")
            .IsUnique()
            .HasDatabaseName("ix_tenant_provider_configs_unique_default_per_category");
    }
}

public class ProviderWebhookInboxConfiguration : IEntityTypeConfiguration<ProviderWebhookInbox>
{
    public void Configure(EntityTypeBuilder<ProviderWebhookInbox> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(w => w.ProviderName).HasMaxLength(64).IsRequired();
        builder.Property(w => w.SignatureHash).HasMaxLength(128).IsRequired();
        builder.Property(w => w.EventType).HasMaxLength(64).IsRequired();
        builder.Property(w => w.PayloadJson).HasMaxLength(32000);
        builder.Property(w => w.ProcessingError).HasMaxLength(2000);
        builder.Property(w => w.ReceivedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(w => w.ProcessedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(w => w.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(w => w.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(w => new { w.TenantId, w.SignatureHash })
            .IsUnique()
            .HasDatabaseName("ix_provider_webhook_inbox_tenant_signature_hash");

        builder.HasIndex(w => new { w.TenantId, w.Category, w.ProcessedAtUtc })
            .HasDatabaseName("ix_provider_webhook_inbox_tenant_category_processed");
    }
}
