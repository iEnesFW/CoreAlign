using CoreAlign.Domain.Entities.Whitelabel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class TenantThemeConfiguration : IEntityTypeConfiguration<TenantTheme>
{
    public void Configure(EntityTypeBuilder<TenantTheme> builder)
    {
        builder.ToTable("tenant_themes");
        builder.HasKey(t => t.TenantId);

        builder.Property(t => t.PrimaryColor).HasMaxLength(16).IsRequired();
        builder.Property(t => t.AccentColor).HasMaxLength(16).IsRequired();
        builder.Property(t => t.BrandName).HasMaxLength(200);
        builder.Property(t => t.CustomSubdomain).HasMaxLength(64);
        builder.Property(t => t.CustomDomain).HasMaxLength(255);
        builder.Property(t => t.EmailFromName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.EmailFromAddress).HasMaxLength(320);
        builder.Property(t => t.LoginHeadingMd).HasMaxLength(2000);

        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(t => t.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(t => t.CustomSubdomain)
            .HasDatabaseName("ux_tenant_themes_custom_subdomain")
            .IsUnique()
            .HasFilter("custom_subdomain IS NOT NULL");

        builder.HasIndex(t => t.CustomDomain)
            .HasDatabaseName("ux_tenant_themes_custom_domain")
            .IsUnique()
            .HasFilter("custom_domain IS NOT NULL");
    }
}

public class TenantThemeAssetConfiguration : IEntityTypeConfiguration<TenantThemeAsset>
{
    public void Configure(EntityTypeBuilder<TenantThemeAsset> builder)
    {
        builder.ToTable("tenant_theme_assets");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssetKind).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.PublicUrl).HasMaxLength(1024);

        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(a => new { a.TenantId, a.AssetKind })
            .HasDatabaseName("ix_tenant_theme_assets_tenant_kind");
    }
}
