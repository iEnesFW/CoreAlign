using CoreAlign.Domain.Entities.Sso;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class TenantIdentityProviderConfiguration : IEntityTypeConfiguration<TenantIdentityProvider>
{
    public void Configure(EntityTypeBuilder<TenantIdentityProvider> builder)
    {
        builder.ToTable("tenant_identity_providers");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Protocol).HasConversion<int>();
        builder.Property(p => p.EntityIdOrClientId).HasMaxLength(512).IsRequired();
        builder.Property(p => p.MetadataUrl).HasMaxLength(1024);
        builder.Property(p => p.DiscoveryDocumentUrl).HasMaxLength(1024);
        builder.Property(p => p.ClientSecretEncrypted).HasMaxLength(2048);
        builder.Property(p => p.AttributeMappingsJson).HasColumnType("text").IsRequired();
        builder.Property(p => p.DeletedReason).HasMaxLength(500);

        builder.Property(p => p.LastUsedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(p => new { p.TenantId, p.Name })
            .IsUnique()
            .HasDatabaseName("ux_tenant_identity_providers_tenant_name");
        builder.HasIndex(p => new { p.TenantId, p.IsActive })
            .HasDatabaseName("ix_tenant_identity_providers_tenant_active");
    }
}

public class ExternalUserBindingConfiguration : IEntityTypeConfiguration<ExternalUserBinding>
{
    public void Configure(EntityTypeBuilder<ExternalUserBinding> builder)
    {
        builder.ToTable("external_user_bindings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.ExternalUserId).HasMaxLength(512).IsRequired();
        builder.Property(b => b.ExternalEmail).HasMaxLength(320);

        builder.Property(b => b.LastLoginAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(b => b.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(b => new { b.IdentityProviderId, b.ExternalUserId })
            .IsUnique()
            .HasDatabaseName("ux_external_user_bindings_idp_external");
        builder.HasIndex(b => new { b.TenantId, b.LocalUserId })
            .HasDatabaseName("ix_external_user_bindings_tenant_user");
    }
}
