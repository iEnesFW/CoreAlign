using System.Text.Json;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).HasMaxLength(64).IsRequired();
        builder.Property(i => i.CustomerNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(i => i.Type).HasMaxLength(20).HasConversion<string>();
        builder.Property(i => i.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(i => i.Subtotal).HasColumnType("numeric(18,4)");
        builder.Property(i => i.LineDiscountTotal).HasColumnType("numeric(18,4)");
        builder.Property(i => i.HeaderDiscountAmount).HasColumnType("numeric(18,4)");
        builder.Property(i => i.HeaderDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(i => i.TaxableTotal).HasColumnType("numeric(18,4)");
        builder.Property(i => i.TaxTotal).HasColumnType("numeric(18,4)");
        builder.Property(i => i.WithholdingTotal).HasColumnType("numeric(18,4)");
        builder.Property(i => i.ShippingCost).HasColumnType("numeric(18,4)");
        builder.Property(i => i.RoundingAdjustment).HasColumnType("numeric(18,4)");
        builder.Property(i => i.Total).HasColumnType("numeric(18,4)");
        builder.Property(i => i.AmountPaid).HasColumnType("numeric(18,4)");
        builder.Property(i => i.IssueDate).HasColumnType("timestamp with time zone");
        builder.Property(i => i.DueDate).HasColumnType("timestamp with time zone");
        builder.Property(i => i.PostingDate).HasColumnType("timestamp with time zone");
        builder.Property(i => i.IssuedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.PaidAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.CancelledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.VoidedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.SentAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.TaxBreakdownJson).HasColumnType("jsonb");
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.Property(i => i.InternalNotes).HasMaxLength(2000);
        builder.Property(i => i.PublicNotes).HasMaxLength(2000);
        builder.Property(i => i.TermsAndConditions).HasMaxLength(4000);
        builder.Property(i => i.CancelReason).HasMaxLength(500);
        builder.Property(i => i.VoidReason).HasMaxLength(500);
        builder.Property(i => i.EInvoiceUuid).HasMaxLength(64);
        builder.Property(i => i.EInvoiceStatus).HasMaxLength(40);
        builder.Property(i => i.EInvoicePdfPath).HasMaxLength(500);

        var jsonOpts = new JsonSerializerOptions();
        builder.Property(i => i.CustomerSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<CustomerSnapshot>(v, jsonOpts));
        builder.Property(i => i.BillingAddressSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<AddressSnapshot>(v, jsonOpts));
        builder.Property(i => i.ShippingAddressSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<AddressSnapshot>(v, jsonOpts));

        builder.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Order).WithMany().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(i => i.Lines).WithOne(l => l.Invoice).HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();
        builder.HasIndex(i => new { i.TenantId, i.CustomerId });
        builder.HasIndex(i => new { i.TenantId, i.Status });
        builder.HasIndex(i => new { i.TenantId, i.DueDate });
        builder.HasIndex(i => new { i.TenantId, i.IssueDate }).IsDescending(false, true);

        builder.Ignore(i => i.AmountDue);
        builder.Ignore(i => i.IsEditable);
        builder.Ignore(i => i.IsIssued);
        builder.Ignore(i => i.IsFinalized);
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(2000);
        builder.Property(l => l.UomCode).HasMaxLength(20);
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.LineDiscountAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.TaxRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.TaxAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.WithholdingRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.WithholdingAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineSubtotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineNetAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineTotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.RevenueAccountCode).HasMaxLength(32);
        builder.Property(l => l.CostCenter).HasMaxLength(64);
        builder.Property(l => l.Project).HasMaxLength(64);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.InvoiceId);
        builder.HasIndex(l => l.ProductId);
        builder.HasIndex(l => l.OriginOrderLineId);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PaymentNumber).HasMaxLength(64).IsRequired();
        builder.Property(p => p.CustomerNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Direction).HasMaxLength(30).HasConversion<string>();
        builder.Property(p => p.Status).HasMaxLength(30).HasConversion<string>();
        builder.Property(p => p.Method).HasMaxLength(30).HasConversion<string>();
        builder.Property(p => p.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(p => p.Amount).HasColumnType("numeric(18,4)");
        builder.Property(p => p.AppliedAmount).HasColumnType("numeric(18,4)");
        builder.Property(p => p.BankAccountInfo).HasMaxLength(200);
        builder.Property(p => p.ReferenceNumber).HasMaxLength(100);
        builder.Property(p => p.CheckNumber).HasMaxLength(40);
        builder.Property(p => p.CheckDueDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.PaymentDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.PostingDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ConfirmedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.VoidedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.VoidReason).HasMaxLength(500);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Applications).WithOne(a => a.Payment).HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.PaymentNumber }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.CustomerId });
        builder.HasIndex(p => new { p.TenantId, p.Status });
        builder.HasIndex(p => new { p.TenantId, p.PaymentDate }).IsDescending(false, true);

        builder.Ignore(p => p.UnappliedAmount);
        builder.Ignore(p => p.IsEditable);
        builder.Ignore(p => p.IsConfirmed);
    }
}

public class PaymentApplicationConfiguration : IEntityTypeConfiguration<PaymentApplication>
{
    public void Configure(EntityTypeBuilder<PaymentApplication> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AppliedAmount).HasColumnType("numeric(18,4)");
        builder.Property(a => a.AppliedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(a => a.Invoice).WithMany().HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.PaymentId });
        builder.HasIndex(a => new { a.TenantId, a.InvoiceId });
    }
}

public class CustomerLedgerEntryConfiguration : IEntityTypeConfiguration<CustomerLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CustomerLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntryType).HasMaxLength(10).HasConversion<string>();
        builder.Property(e => e.SourceType).HasMaxLength(30).HasConversion<string>();
        builder.Property(e => e.SourceDocumentNumber).HasMaxLength(64);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(e => e.Amount).HasColumnType("numeric(18,4)");
        builder.Property(e => e.AmountInBase).HasColumnType("numeric(18,4)");
        builder.Property(e => e.RunningBalanceAfter).HasColumnType("numeric(18,4)");
        builder.Property(e => e.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.PostingDate).HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.CustomerId, e.OccurredAtUtc }).IsDescending(false, false, true);
        builder.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceDocumentId });

        builder.Ignore(e => e.SignedAmount);
    }
}
