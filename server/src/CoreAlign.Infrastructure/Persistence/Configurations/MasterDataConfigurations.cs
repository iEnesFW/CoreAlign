using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).HasMaxLength(32).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(1000);
        builder.Property(b => b.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(b => new { b.TenantId, b.Code }).IsUnique();
        builder.HasIndex(b => new { b.TenantId, b.Name });
    }
}

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.ParentCategoryId });
    }
}

public class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Code).HasMaxLength(32).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(150).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(1000);
        builder.Property(g => g.DefaultDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(g => g.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(g => g.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(g => new { g.TenantId, g.Code }).IsUnique();
    }
}

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Code).HasMaxLength(20).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Symbol).HasMaxLength(10);
        builder.Property(u => u.ConversionFactor).HasColumnType("numeric(18,6)");
        builder.Property(u => u.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(u => u.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(u => u.BaseUom)
            .WithMany()
            .HasForeignKey(u => u.BaseUomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => new { u.TenantId, u.Code }).IsUnique();

        builder.Ignore(u => u.IsBase);
    }
}

public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(32).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.RatePercent).HasColumnType("numeric(6,3)");
        builder.Property(t => t.CountryCode).HasMaxLength(3);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.IsWithholding });
    }
}

public class PaymentTermConfiguration : IEntityTypeConfiguration<PaymentTerm>
{
    public void Configure(EntityTypeBuilder<PaymentTerm> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.DiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique();
    }
}

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.ValidFromUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ValidUntilUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(p => p.Items)
            .WithOne(i => i.PriceList)
            .HasForeignKey(i => i.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.IsDefault });
    }
}

public class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Price).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(i => i.MinQuantity).HasColumnType("numeric(18,4)");
        builder.Property(i => i.MaxQuantity).HasColumnType("numeric(18,4)");
        builder.Property(i => i.DiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(i => i.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.TenantId, i.PriceListId, i.ProductId });
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Code).HasMaxLength(32).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.AddressLine1).HasMaxLength(200);
        builder.Property(w => w.AddressLine2).HasMaxLength(200);
        builder.Property(w => w.City).HasMaxLength(100);
        builder.Property(w => w.State).HasMaxLength(100);
        builder.Property(w => w.PostalCode).HasMaxLength(20);
        builder.Property(w => w.Country).HasMaxLength(3);
        builder.Property(w => w.Phone).HasMaxLength(30);
        builder.Property(w => w.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(w => w.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(w => new { w.TenantId, w.Code }).IsUnique();
        builder.HasIndex(w => new { w.TenantId, w.IsDefault });
    }
}

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(b => b.BankName).HasMaxLength(200).IsRequired();
        builder.Property(b => b.BranchName).HasMaxLength(100);
        builder.Property(b => b.Iban).HasMaxLength(34).IsRequired();
        builder.Property(b => b.Swift).HasMaxLength(11);
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired();
        builder.Property(b => b.OpeningBalance).HasPrecision(18, 4);
        builder.Property(b => b.Notes).HasMaxLength(1000);
        builder.Property(b => b.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(b => new { b.TenantId, b.Iban }).IsUnique();
        builder.HasIndex(b => new { b.TenantId, b.IsActive });
    }
}

public class DunningSettingConfiguration : IEntityTypeConfiguration<DunningSetting>
{
    public void Configure(EntityTypeBuilder<DunningSetting> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(d => d.RecipientUserIdsJson).IsRequired();
        builder.Property(d => d.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(d => new { d.TenantId, d.Type }).IsUnique();
    }
}

public class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(d => d.Prefix).HasMaxLength(16).IsRequired();
        builder.Property(d => d.Format).HasMaxLength(64);
        builder.Property(d => d.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(d => d.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(d => new { d.TenantId, d.Type }).IsUnique();
    }
}
