using CoreAlign.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("notification_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Channel).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.TemplateKey).HasMaxLength(128).IsRequired();
        builder.Property(m => m.Locale).HasMaxLength(8).IsRequired();
        builder.Property(m => m.RecipientAddress).HasMaxLength(320).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(500);
        builder.Property(m => m.BodyMarkdown).HasMaxLength(32000).IsRequired();
        builder.Property(m => m.PayloadJson).HasMaxLength(16000).IsRequired();
        builder.Property(m => m.CategoryKey).HasMaxLength(64).IsRequired();
        builder.Property(m => m.FailureReason).HasMaxLength(2000);
        builder.Property(m => m.ProviderUsed).HasMaxLength(64);
        builder.Property(m => m.ProviderMessageId).HasMaxLength(256);
        builder.Property(m => m.IdempotencyHash).HasMaxLength(64).IsRequired().HasDefaultValue(string.Empty);

        builder.Property(m => m.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.SentAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.DeliveredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.ReadAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(m => m.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(m => m.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(m => new { m.TenantId, m.Status })
            .HasDatabaseName("ix_notification_messages_tenant_status");
        builder.HasIndex(m => new { m.TenantId, m.UserId })
            .HasDatabaseName("ix_notification_messages_tenant_user");
        builder.HasIndex(m => new { m.TenantId, m.CustomerId })
            .HasDatabaseName("ix_notification_messages_tenant_customer");
        builder.HasIndex(m => new { m.TenantId, m.CategoryKey })
            .HasDatabaseName("ix_notification_messages_tenant_category");
        builder.HasIndex(m => new { m.ProviderUsed, m.ProviderMessageId })
            .HasDatabaseName("ix_notification_messages_provider_msg");
        builder.HasIndex(m => new { m.TenantId, m.IdempotencyHash })
            .HasDatabaseName("ux_notification_messages_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_hash <> ''");
    }
}

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Key).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Channel).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.Locale).HasMaxLength(8).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(500);
        builder.Property(t => t.BodyTemplate).HasMaxLength(32000).IsRequired();

        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.DeletedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(t => t.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(t => new { t.TenantId, t.Key, t.Channel, t.Locale })
            .HasDatabaseName("ux_notification_templates_tenant_key_channel_locale")
            .IsUnique();
    }
}

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CategoryKey).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Channel).HasConversion<string>().HasMaxLength(16);

        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.UserId, p.CategoryKey, p.Channel })
            .HasDatabaseName("ux_notification_preferences_user_category_channel")
            .IsUnique();
    }
}

public class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.ToTable("user_device_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).HasMaxLength(512).IsRequired();
        builder.Property(t => t.Platform).HasMaxLength(16).IsRequired();
        builder.Property(t => t.DeviceName).HasMaxLength(256);
        builder.Property(t => t.OsVersion).HasMaxLength(64);
        builder.Property(t => t.IsActive).HasDefaultValue(true);

        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.LastSeenAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(t => new { t.TenantId, t.UserId })
            .HasDatabaseName("ix_user_device_tokens_tenant_user");
        builder.HasIndex(t => new { t.TenantId, t.Token })
            .HasDatabaseName("ux_user_device_tokens_tenant_token")
            .IsUnique();
    }
}
