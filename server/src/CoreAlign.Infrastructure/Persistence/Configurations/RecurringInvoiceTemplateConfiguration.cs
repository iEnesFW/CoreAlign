using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class RecurringInvoiceTemplateConfiguration : IEntityTypeConfiguration<RecurringInvoiceTemplate>
{
    public void Configure(EntityTypeBuilder<RecurringInvoiceTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.HeaderDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(t => t.HeaderDiscountAmount).HasColumnType("numeric(18,4)");
        builder.Property(t => t.ShippingCost).HasColumnType("numeric(18,4)");
        builder.Property(t => t.RoundingAdjustment).HasColumnType("numeric(18,4)");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(t => t.Lines)
            .WithOne(l => l.Template!)
            .HasForeignKey(l => l.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Occurrences)
            .WithOne(o => o.Template!)
            .HasForeignKey(o => o.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TenantId, t.CustomerId });
        builder.HasIndex(t => t.NextRunDate).HasFilter("status = 0");
    }
}

public class RecurringInvoiceTemplateLineConfiguration : IEntityTypeConfiguration<RecurringInvoiceTemplateLine>
{
    public void Configure(EntityTypeBuilder<RecurringInvoiceTemplateLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ProductSku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitPrice).HasColumnType("numeric(18,4)");
        builder.Property(l => l.TaxRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.LineDiscountPercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.LineDiscountAmount).HasColumnType("numeric(18,4)");
        builder.Property(l => l.WithholdingRatePercent).HasColumnType("numeric(6,3)");
        builder.Property(l => l.UomCode).HasMaxLength(16);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(l => new { l.TenantId, l.TemplateId });
    }
}

public class RecurringInvoiceOccurrenceConfiguration : IEntityTypeConfiguration<RecurringInvoiceOccurrence>
{
    public void Configure(EntityTypeBuilder<RecurringInvoiceOccurrence> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.GeneratedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(o => o.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(o => o.GeneratedInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.TenantId, o.TemplateId, o.PeriodKey }).IsUnique();
    }
}
