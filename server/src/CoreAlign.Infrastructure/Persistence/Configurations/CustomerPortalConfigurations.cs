using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class PaymentSessionConfiguration : IEntityTypeConfiguration<PaymentSession>
{
    public void Configure(EntityTypeBuilder<PaymentSession> builder)
    {
        builder.ToTable("payment_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.GatewayName).HasMaxLength(32).IsRequired();
        builder.Property(s => s.IntentId).HasMaxLength(128).IsRequired();
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.Amount).HasColumnType("numeric(18,4)");
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.RedirectUrl).HasMaxLength(2048);
        builder.Property(s => s.ProviderReference).HasMaxLength(128);
        builder.Property(s => s.FailureReason).HasMaxLength(500);
        builder.Property(s => s.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(s => new { s.GatewayName, s.IntentId }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.InvoiceId });
        builder.HasIndex(s => new { s.TenantId, s.CustomerId, s.Status });
    }
}

public class UserNotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
    {
        builder.ToTable("user_notification_preferences");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.NotificationKind).HasMaxLength(64).IsRequired();
        builder.Property(p => p.EmailEnabled);
        builder.Property(p => p.InAppEnabled);
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.UserId, p.NotificationKind }).IsUnique();
    }
}
