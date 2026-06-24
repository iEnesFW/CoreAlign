using System.Text.Json;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuoteNumber).HasMaxLength(64).IsRequired();
        builder.Property(q => q.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(q => q.Currency).HasMaxLength(3).IsRequired();
        builder.Property(q => q.ExchangeRate).HasColumnType("numeric(18,6)");
        builder.Property(q => q.Subtotal).HasColumnType("numeric(18,4)");
        builder.Property(q => q.LineDiscountTotal).HasColumnType("numeric(18,4)");
        builder.Property(q => q.HeaderDiscountAmount).HasColumnType("numeric(18,4)");
        builder.Property(q => q.HeaderDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(q => q.TaxableTotal).HasColumnType("numeric(18,4)");
        builder.Property(q => q.TaxTotal).HasColumnType("numeric(18,4)");
        builder.Property(q => q.WithholdingTotal).HasColumnType("numeric(18,4)");
        builder.Property(q => q.ShippingCost).HasColumnType("numeric(18,4)");
        builder.Property(q => q.RoundingAdjustment).HasColumnType("numeric(18,4)");
        builder.Property(q => q.Total).HasColumnType("numeric(18,4)");
        builder.Property(q => q.Notes).HasMaxLength(2000);
        builder.Property(q => q.InternalNotes).HasMaxLength(2000);
        builder.Property(q => q.CustomerNotes).HasMaxLength(2000);
        builder.Property(q => q.PublicNotes).HasMaxLength(2000);
        builder.Property(q => q.TermsAndConditions).HasMaxLength(4000);
        builder.Property(q => q.RejectionReason).HasMaxLength(500);
        builder.Property(q => q.QuoteDate).HasColumnType("timestamp with time zone");
        builder.Property(q => q.ValidUntilUtc).HasColumnType("timestamp with time zone");
        builder.Property(q => q.SentAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(q => q.AcceptedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(q => q.RejectedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(q => q.ExpiredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(q => q.ConvertedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(q => q.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(q => q.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        var jsonOpts = new JsonSerializerOptions();
        builder.Property(q => q.CustomerSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<CustomerSnapshot>(v, jsonOpts));
        builder.Property(q => q.BillingAddressSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<AddressSnapshot>(v, jsonOpts));
        builder.Property(q => q.ShippingAddressSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOpts),
                v => v == null ? null : JsonSerializer.Deserialize<AddressSnapshot>(v, jsonOpts));

        builder.HasOne(q => q.Customer).WithMany().HasForeignKey(q => q.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(q => q.Lines).WithOne(l => l.Quote).HasForeignKey(l => l.QuoteId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => new { q.TenantId, q.QuoteNumber }).IsUnique();
        builder.HasIndex(q => new { q.TenantId, q.CustomerId });
        builder.HasIndex(q => new { q.TenantId, q.Status });
        builder.HasIndex(q => new { q.TenantId, q.ValidUntilUtc, q.Status });

        builder.Ignore(q => q.IsDraft);
        builder.Ignore(q => q.IsEditable);
        builder.Ignore(q => q.IsTerminal);
    }
}

public class QuoteLineConfiguration : IEntityTypeConfiguration<QuoteLine>
{
    public void Configure(EntityTypeBuilder<QuoteLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.ProductDescriptionSnapshot).HasMaxLength(2000);
        builder.Property(l => l.UomCode).HasMaxLength(20);
        builder.Property(l => l.UomConversionFactor).HasColumnType("numeric(18,6)");
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(l => l.ListPriceSnapshot).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.LineDiscountAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.TaxRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.TaxAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.WithholdingRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.WithholdingAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineSubtotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineNetAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineTotal).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineNotes).HasMaxLength(1000);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.QuoteId);
        builder.HasIndex(l => l.ProductId);
        builder.Ignore(l => l.LineTaxAmount);
        builder.Ignore(l => l.LineWithholdingAmount);
    }
}
