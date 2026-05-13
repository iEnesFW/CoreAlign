using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(32);
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.LegalName).HasMaxLength(200);
        builder.Property(c => c.TradeName).HasMaxLength(200);
        builder.Property(c => c.NationalId).HasMaxLength(32);
        builder.Property(c => c.TaxNumber).HasMaxLength(50);
        builder.Property(c => c.TaxOffice).HasMaxLength(100);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Website).HasMaxLength(500);
        builder.Property(c => c.DefaultCurrency).HasMaxLength(3).IsRequired();
        builder.Property(c => c.CreditLimit).HasColumnType("numeric(18,4)");
        builder.Property(c => c.CurrentBalance).HasColumnType("numeric(18,4)");
        builder.Property(c => c.OverdueAmount).HasColumnType("numeric(18,4)");
        builder.Property(c => c.DefaultDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(c => c.Classification).HasMaxLength(16);
        builder.Property(c => c.Channel).HasMaxLength(32);
        builder.Property(c => c.Territory).HasMaxLength(64);
        builder.Property(c => c.LanguageCode).HasMaxLength(5);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.BlockReason).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(c => c.PaymentTerms).WithMany().HasForeignKey(c => c.PaymentTermsId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.PriceList).WithMany().HasForeignKey(c => c.PriceListId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.CustomerGroup).WithMany().HasForeignKey(c => c.CustomerGroupId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.ParentCustomer).WithMany().HasForeignKey(c => c.ParentCustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.Code })
            .IsUnique()
            .HasFilter("code IS NOT NULL")
            .HasDatabaseName("ix_customers_tenant_code_unique");
        builder.HasIndex(c => new { c.TenantId, c.Name });
        builder.HasIndex(c => new { c.TenantId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.CustomerGroupId });
        builder.HasIndex(c => new { c.TenantId, c.Email })
            .IsUnique()
            .HasFilter("email IS NOT NULL")
            .HasDatabaseName("ix_customers_tenant_email_unique");
        builder.HasIndex(c => new { c.TenantId, c.TaxNumber })
            .IsUnique()
            .HasFilter("tax_number IS NOT NULL")
            .HasDatabaseName("ix_customers_tenant_tax_number_unique");

        builder.Ignore(c => c.IsActive);
    }
}
