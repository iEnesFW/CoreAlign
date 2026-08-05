using CoreAlign.Domain.Entities.GlassEnclosure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class WindZoneConfiguration : IEntityTypeConfiguration<WindZone>
{
    public void Configure(EntityTypeBuilder<WindZone> builder)
    {
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Code).HasMaxLength(32).IsRequired();
        builder.Property(z => z.RegionLabelTr).HasMaxLength(200).IsRequired();
        builder.Property(z => z.RegionLabelEn).HasMaxLength(200).IsRequired();
        builder.Property(z => z.BaseWindPressurePa).HasColumnType("numeric(10,2)");
        builder.Property(z => z.BasicWindSpeedMs).HasColumnType("numeric(6,2)");
        builder.Property(z => z.HeightFactorMultiplier).HasColumnType("numeric(8,4)");
        builder.Property(z => z.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(z => z.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(z => z.Code).IsUnique();
        builder.HasIndex(z => z.IsActive);
    }
}

public class ClimateZoneConfiguration : IEntityTypeConfiguration<ClimateZone>
{
    public void Configure(EntityTypeBuilder<ClimateZone> builder)
    {
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Code).HasMaxLength(32).IsRequired();
        builder.Property(z => z.NameTr).HasMaxLength(200).IsRequired();
        builder.Property(z => z.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(z => z.AvgWinterTemperatureC).HasColumnType("numeric(6,2)");
        builder.Property(z => z.AvgHumidityPercent).HasColumnType("numeric(5,2)");
        builder.Property(z => z.CorrosionClass).HasConversion<string>().HasMaxLength(8);
        builder.Property(z => z.IlPostalPrefixListJson).HasColumnType("jsonb");
        builder.Property(z => z.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(z => z.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(z => z.Code).IsUnique();
        builder.HasIndex(z => z.IsActive);
    }
}

public class ColorOptionConfiguration : IEntityTypeConfiguration<ColorOption>
{
    public void Configure(EntityTypeBuilder<ColorOption> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.RalCode).HasMaxLength(16);
        builder.Property(c => c.HexColor).HasMaxLength(8).IsRequired();
        builder.Property(c => c.FinishType).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.PriceModifierPercent).HasColumnType("numeric(6,3)");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.IsActive });
    }
}

public class GlassTypeConfiguration : IEntityTypeConfiguration<GlassType>
{
    public void Configure(EntityTypeBuilder<GlassType> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Code).HasMaxLength(32).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Structure).HasConversion<string>().HasMaxLength(20);
        builder.Property(g => g.GlassLayersJson).HasColumnType("jsonb");
        builder.Property(g => g.UValue).HasColumnType("numeric(6,3)");
        builder.Property(g => g.SoundDb).HasColumnType("numeric(6,2)");
        builder.Property(g => g.MaxPanelAreaM2).HasColumnType("numeric(10,3)");
        builder.Property(g => g.AllowablePressurePa).HasColumnType("numeric(10,2)");
        builder.Property(g => g.WeightKgPerM2).HasColumnType("numeric(10,3)");
        builder.Property(g => g.PricePerM2).HasColumnType("numeric(18,4)");
        builder.Property(g => g.Currency).HasMaxLength(3).IsRequired();
        builder.Property(g => g.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(g => g.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(g => new { g.TenantId, g.Code }).IsUnique();
        builder.HasIndex(g => new { g.TenantId, g.Structure, g.IsActive });
    }
}

public class ProfileSystemConfiguration : IEntityTypeConfiguration<ProfileSystem>
{
    public void Configure(EntityTypeBuilder<ProfileSystem> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.SystemType).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.SupportedGlassThicknessesJson).HasColumnType("jsonb");
        builder.Property(s => s.SupportedOpeningsJson).HasColumnType("jsonb");
        builder.Property(s => s.CertificationClass).HasMaxLength(100);
        builder.Property(s => s.FireClass).HasMaxLength(50);
        builder.Property(s => s.ThermalUValue).HasColumnType("numeric(6,3)");
        builder.Property(s => s.ThermalBreakFactor).HasColumnType("numeric(6,3)");
        builder.Property(s => s.MaxPanelWeightKg).HasColumnType("numeric(10,2)");
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SystemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.TenantId, s.Code }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.BrandId });
        builder.HasIndex(s => new { s.TenantId, s.SystemType, s.IsActive });
    }
}

public class ProfileItemConfiguration : IEntityTypeConfiguration<ProfileItem>
{
    public void Configure(EntityTypeBuilder<ProfileItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Code).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.WeightKgPerMeter).HasColumnType("numeric(10,4)");
        builder.Property(i => i.PricePerKg).HasColumnType("numeric(18,4)");
        builder.Property(i => i.ReorderPointMeters).HasColumnType("numeric(12,2)");
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.CrossSectionSvg).HasColumnType("text");
        builder.Property(i => i.CrossSectionDxfUrl).HasMaxLength(500);
        builder.Property(i => i.ParametricDescriptionJson).HasColumnType("jsonb");
        builder.Property(i => i.VendorPartNumber).HasMaxLength(64);
        builder.Property(i => i.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(i => new { i.TenantId, i.Code }).IsUnique();
        builder.HasIndex(i => new { i.TenantId, i.SystemId, i.Role });
    }
}

public class HardwareItemConfiguration : IEntityTypeConfiguration<HardwareItem>
{
    public void Configure(EntityTypeBuilder<HardwareItem> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Code).HasMaxLength(64).IsRequired();
        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.Unit).HasMaxLength(20).IsRequired();
        builder.Property(h => h.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(h => h.Currency).HasMaxLength(3).IsRequired();
        builder.Property(h => h.MaxLoadKg).HasColumnType("numeric(10,2)");
        builder.Property(h => h.CompatibleSystemIdsJson).HasColumnType("jsonb");
        builder.Property(h => h.ModelGlbUrl).HasMaxLength(500);
        builder.Property(h => h.VendorPartNumber).HasMaxLength(64);
        builder.Property(h => h.ReorderPointQuantity).HasColumnType("numeric(12,2)");
        builder.Property(h => h.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(h => h.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(h => new { h.TenantId, h.Code }).IsUnique();
        builder.HasIndex(h => new { h.TenantId, h.Category, h.IsActive });
        builder.HasIndex(h => new { h.TenantId, h.BrandId });
    }
}

public class HardwareKitConfiguration : IEntityTypeConfiguration<HardwareKit>
{
    public void Configure(EntityTypeBuilder<HardwareKit> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Code).HasMaxLength(64).IsRequired();
        builder.Property(k => k.Name).HasMaxLength(200).IsRequired();
        builder.Property(k => k.Description).HasMaxLength(2000);
        builder.Property(k => k.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(k => k.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(k => k.Items)
            .WithOne()
            .HasForeignKey(i => i.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(k => new { k.TenantId, k.Code }).IsUnique();
        builder.HasIndex(k => new { k.TenantId, k.SystemId });
    }
}

public class HardwareKitItemConfiguration : IEntityTypeConfiguration<HardwareKitItem>
{
    public void Configure(EntityTypeBuilder<HardwareKitItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.QuantityFormula).HasMaxLength(500).IsRequired();
        builder.Property(i => i.ConditionExpression).HasMaxLength(500);
        builder.Property(i => i.Note).HasMaxLength(1000);
        builder.Property(i => i.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(i => new { i.TenantId, i.KitId, i.SortOrder });
        builder.HasIndex(i => new { i.TenantId, i.HardwareItemId });
    }
}

public class BrandVendorConfiguration : IEntityTypeConfiguration<BrandVendor>
{
    public void Configure(EntityTypeBuilder<BrandVendor> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.DefaultPaymentTerms).HasMaxLength(200);
        builder.Property(b => b.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(b => new { b.TenantId, b.BrandId, b.VendorId }).IsUnique();
        builder.HasIndex(b => new { b.TenantId, b.BrandId, b.IsPreferred });
    }
}

public class DiscountRuleConfiguration : IEntityTypeConfiguration<DiscountRule>
{
    public void Configure(EntityTypeBuilder<DiscountRule> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).HasMaxLength(64).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Scope).HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.DiscountKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.CouponCode).HasMaxLength(64);
        builder.Property(d => d.DiscountValue).HasColumnType("numeric(18,4)");
        builder.Property(d => d.MinAreaM2).HasColumnType("numeric(10,3)");
        builder.Property(d => d.ValidFromUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.ValidUntilUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(d => new { d.TenantId, d.Code }).IsUnique();
        builder.HasIndex(d => new { d.TenantId, d.CouponCode }).IsUnique()
            .HasFilter("coupon_code IS NOT NULL");
        builder.HasIndex(d => new { d.TenantId, d.Scope, d.IsActive });
    }
}

public class GlassNotificationTemplateConfiguration : IEntityTypeConfiguration<GlassNotificationTemplate>
{
    public void Configure(EntityTypeBuilder<GlassNotificationTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(64).IsRequired();
        builder.Property(t => t.EventCode).HasConversion<string>().HasMaxLength(40);
        builder.Property(t => t.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Locale).HasMaxLength(10).IsRequired();
        builder.Property(t => t.SubjectTemplate).HasMaxLength(500);
        builder.Property(t => t.BodyTemplate).HasColumnType("text");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.EventCode, t.Channel, t.Locale }).IsUnique();
    }
}

public class GlassEnclosureSettingsConfiguration : IEntityTypeConfiguration<GlassEnclosureSettings>
{
    public void Configure(EntityTypeBuilder<GlassEnclosureSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SawKerfMm).HasColumnType("numeric(8,3)");
        builder.Property(s => s.GlassKerfMm).HasColumnType("numeric(8,3)");
        builder.Property(s => s.DefaultWastePercent).HasColumnType("numeric(6,3)");
        builder.Property(s => s.LaborCostPerM2).HasColumnType("numeric(18,4)");
        builder.Property(s => s.DefaultMarginPercent).HasColumnType("numeric(6,3)");
        builder.Property(s => s.DefaultTaxRatePercent).HasColumnType("numeric(6,3)").HasDefaultValue(20m);
        builder.Property(s => s.BendRailFeePerM).HasColumnType("numeric(18,4)").HasDefaultValue(150m);
        builder.Property(s => s.BentGlassCostFactor).HasColumnType("numeric(6,3)").HasDefaultValue(2.75m);
        builder.Property(s => s.TransportRatePerKm).HasColumnType("numeric(18,4)");
        builder.Property(s => s.TransportRatePerKg).HasColumnType("numeric(18,4)");
        builder.Property(s => s.ScaffoldingRatePerM2).HasColumnType("numeric(18,4)");
        builder.Property(s => s.CraneRatePerMeter).HasColumnType("numeric(18,4)");
        builder.Property(s => s.WorkshopDailyCapacityM2).HasColumnType("numeric(10,2)");
        builder.Property(s => s.DefaultLocale).HasMaxLength(10).IsRequired();
        builder.Property(s => s.DefaultCurrency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.DefaultPaymentTermsJson).HasColumnType("jsonb");
        builder.Property(s => s.WhatsappBusinessPhoneId).HasMaxLength(100);
        builder.Property(s => s.NotificationEmailFrom).HasMaxLength(255);
        builder.Property(s => s.OnboardingStateJson).HasColumnType("jsonb");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(s => s.TenantId).IsUnique();
    }
}
