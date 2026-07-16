using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public sealed class IncomingInvoiceConfiguration : IEntityTypeConfiguration<IncomingInvoice>
{
    public void Configure(EntityTypeBuilder<IncomingInvoice> builder)
    {
        builder.ToTable("incoming_invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Ettn).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SenderVkn).HasMaxLength(16).IsRequired();
        builder.Property(x => x.SenderName).HasMaxLength(300);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(64);
        builder.Property(x => x.IssueDate).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ProviderName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProviderStatus).HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ProcessedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.TenantId, x.Ettn }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status, x.IssueDate }).IsDescending(false, false, true);

        builder.HasOne<VendorBill>().WithMany().HasForeignKey(x => x.LinkedVendorBillId).OnDelete(DeleteBehavior.SetNull);
    }
}
