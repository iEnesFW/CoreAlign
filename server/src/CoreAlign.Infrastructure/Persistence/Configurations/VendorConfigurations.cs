using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Code).HasMaxLength(32);
        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.LegalName).HasMaxLength(200);
        builder.Property(v => v.TradeName).HasMaxLength(200);
        builder.Property(v => v.NationalId).HasMaxLength(32);
        builder.Property(v => v.TaxNumber).HasMaxLength(50);
        builder.Property(v => v.TaxOffice).HasMaxLength(100);
        builder.Property(v => v.Email).HasMaxLength(256);
        builder.Property(v => v.Phone).HasMaxLength(30);
        builder.Property(v => v.Website).HasMaxLength(500);
        builder.Property(v => v.DefaultCurrency).HasMaxLength(3).IsRequired();
        builder.Property(v => v.CurrentBalance).HasColumnType("numeric(18,4)");
        builder.Property(v => v.OverdueAmount).HasColumnType("numeric(18,4)");
        builder.Property(v => v.TotalPayable).HasColumnType("numeric(18,4)");
        builder.Property(v => v.Classification).HasMaxLength(64);
        builder.Property(v => v.Territory).HasMaxLength(64);
        builder.Property(v => v.LanguageCode).HasMaxLength(10);
        builder.Property(v => v.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(v => v.BlockReason).HasMaxLength(500);
        builder.Property(v => v.Notes).HasMaxLength(2000);
        builder.Property(v => v.DefaultLeadTimeDays).HasDefaultValue(0);
        builder.Property(v => v.ApprovedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(v => v.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(v => v.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(v => v.PaymentTerms).WithMany().HasForeignKey(v => v.PaymentTermsId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(v => v.ParentVendor).WithMany().HasForeignKey(v => v.ParentVendorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.TenantId, v.Code }).IsUnique().HasFilter("\"code\" IS NOT NULL");
        builder.HasIndex(v => new { v.TenantId, v.TaxNumber }).HasFilter("\"tax_number\" IS NOT NULL");
        builder.HasIndex(v => new { v.TenantId, v.Status });
        builder.HasIndex(v => new { v.TenantId, v.Name });

        builder.Ignore(v => v.IsActive);
        builder.Ignore(v => v.CanReceivePO);
    }
}

public class VendorAddressConfiguration : IEntityTypeConfiguration<VendorAddress>
{
    public void Configure(EntityTypeBuilder<VendorAddress> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Label).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Line1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Line2).HasMaxLength(200);
        builder.Property(a => a.City).HasMaxLength(100);
        builder.Property(a => a.State).HasMaxLength(100);
        builder.Property(a => a.PostalCode).HasMaxLength(20);
        builder.Property(a => a.Country).HasMaxLength(100);
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(a => a.Vendor).WithMany().HasForeignKey(a => a.VendorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(a => new { a.TenantId, a.VendorId });
    }
}

public class VendorContactConfiguration : IEntityTypeConfiguration<VendorContact>
{
    public void Configure(EntityTypeBuilder<VendorContact> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Role).HasMaxLength(100);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(c => c.Vendor).WithMany().HasForeignKey(c => c.VendorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => new { c.TenantId, c.VendorId });
    }
}

public class VendorBankAccountConfiguration : IEntityTypeConfiguration<VendorBankAccount>
{
    public void Configure(EntityTypeBuilder<VendorBankAccount> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BankName).HasMaxLength(150).IsRequired();
        builder.Property(b => b.BranchName).HasMaxLength(150);
        builder.Property(b => b.AccountHolder).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Iban).HasColumnType("text").IsRequired();
        builder.Property(b => b.Swift).HasColumnType("text");
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired();
        builder.Property(b => b.AccountNumber).HasColumnType("text");
        builder.Property(b => b.Notes).HasMaxLength(500);
        builder.Property(b => b.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(b => b.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(b => b.Vendor).WithMany().HasForeignKey(b => b.VendorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(b => new { b.TenantId, b.VendorId });
    }
}

public class VendorLedgerEntryConfiguration : IEntityTypeConfiguration<VendorLedgerEntry>
{
    public void Configure(EntityTypeBuilder<VendorLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(8);
        builder.Property(e => e.Amount).HasColumnType("numeric(18,4)");
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(e => e.AmountInBase).HasColumnType("numeric(18,4)");
        builder.Property(e => e.SourceType).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.SourceDocumentNumber).HasMaxLength(64);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.RunningBalanceAfter).HasColumnType("numeric(18,4)");
        builder.Property(e => e.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.PostingDate).HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(e => e.Vendor).WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.VendorId, e.PostingDate });
        builder.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceDocumentId });
        builder.Ignore(e => e.SignedAmount);
    }
}
