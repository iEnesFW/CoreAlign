using CoreAlign.Domain.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.OrderReference).HasMaxLength(128).IsRequired();
        builder.Property(t => t.ProviderName).HasMaxLength(64).IsRequired();
        builder.Property(t => t.ExternalTransactionId).HasMaxLength(128);
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(18, 4);
        builder.Property(t => t.RefundedAmount).HasPrecision(18, 4);

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(t => t.RedirectUrl).HasMaxLength(2000);
        builder.Property(t => t.FailureCode).HasMaxLength(64);
        builder.Property(t => t.FailureReason).HasMaxLength(2000);
        builder.Property(t => t.MetadataJson).HasMaxLength(32000);
        builder.Property(t => t.IdempotencyKey).HasMaxLength(128);

        builder.Property(t => t.AttemptedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(t => t.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(t => new { t.TenantId, t.ProviderName, t.ExternalTransactionId })
            .HasDatabaseName("ix_payment_transactions_tenant_provider_external");

        builder.HasIndex(t => new { t.TenantId, t.Status })
            .HasDatabaseName("ix_payment_transactions_tenant_status");

        builder.HasIndex(t => new { t.TenantId, t.OrderReference })
            .HasDatabaseName("ix_payment_transactions_tenant_orderref");

        builder.HasIndex(t => new { t.TenantId, t.IdempotencyKey })
            .HasDatabaseName("ux_payment_transactions_tenant_idempotency_key")
            .IsUnique()
            .HasFilter("\"idempotency_key\" IS NOT NULL");
    }
}
