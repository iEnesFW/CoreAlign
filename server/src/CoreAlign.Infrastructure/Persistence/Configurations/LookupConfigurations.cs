using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

// Global reference lookups: no ITenantOwned, so no tenant query filter is applied.
// Seeded with a handful of rows; full ISO/TR datasets are added out-of-band.

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> b)
    {
        b.ToTable("currencies");
        b.HasKey(c => c.Code);
        b.Property(c => c.Code).HasMaxLength(3).ValueGeneratedNever();
        b.Property(c => c.Name).HasMaxLength(64).IsRequired();
        b.Property(c => c.Symbol).HasMaxLength(8);

        b.HasData(
            new { Code = "TRY", Name = "Türk Lirası", Symbol = (string?)"₺", IsActive = true },
            new { Code = "USD", Name = "ABD Doları", Symbol = (string?)"$", IsActive = true },
            new { Code = "EUR", Name = "Euro", Symbol = (string?)"€", IsActive = true },
            new { Code = "GBP", Name = "İngiliz Sterlini", Symbol = (string?)"£", IsActive = true });
    }
}

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> b)
    {
        b.ToTable("countries");
        b.HasKey(c => c.Code);
        b.Property(c => c.Code).HasMaxLength(2).ValueGeneratedNever();
        b.Property(c => c.Name).HasMaxLength(128).IsRequired();
        b.Property(c => c.DialCode).HasMaxLength(8);

        b.HasData(
            new { Code = "TR", Name = "Türkiye", DialCode = (string?)"+90", IsActive = true },
            new { Code = "US", Name = "United States", DialCode = (string?)"+1", IsActive = true },
            new { Code = "DE", Name = "Germany", DialCode = (string?)"+49", IsActive = true },
            new { Code = "GB", Name = "United Kingdom", DialCode = (string?)"+44", IsActive = true },
            new { Code = "FR", Name = "France", DialCode = (string?)"+33", IsActive = true });
    }
}

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> b)
    {
        b.ToTable("provinces");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.CountryCode).HasMaxLength(2).IsRequired();
        b.Property(p => p.Name).HasMaxLength(64).IsRequired();
        b.HasIndex(p => p.CountryCode);

        b.HasData(
            new { Id = 6, CountryCode = "TR", Name = "Ankara", IsActive = true },
            new { Id = 16, CountryCode = "TR", Name = "Bursa", IsActive = true },
            new { Id = 34, CountryCode = "TR", Name = "İstanbul", IsActive = true },
            new { Id = 35, CountryCode = "TR", Name = "İzmir", IsActive = true });
    }
}

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> b)
    {
        b.ToTable("districts");
        b.HasKey(d => d.Id);
        b.Property(d => d.Id).ValueGeneratedNever();
        b.Property(d => d.Name).HasMaxLength(64).IsRequired();
        b.HasIndex(d => d.ProvinceId);
        b.HasOne<Province>()
            .WithMany()
            .HasForeignKey(d => d.ProvinceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasData(
            new { Id = 601, ProvinceId = 6, Name = "Çankaya", IsActive = true },
            new { Id = 602, ProvinceId = 6, Name = "Keçiören", IsActive = true },
            new { Id = 1601, ProvinceId = 16, Name = "Osmangazi", IsActive = true },
            new { Id = 3401, ProvinceId = 34, Name = "Kadıköy", IsActive = true },
            new { Id = 3402, ProvinceId = 34, Name = "Beşiktaş", IsActive = true },
            new { Id = 3403, ProvinceId = 34, Name = "Şişli", IsActive = true },
            new { Id = 3501, ProvinceId = 35, Name = "Konak", IsActive = true });
    }
}
