using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.EditedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.EntityType).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Body).HasMaxLength(4000).IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.EntityType, c.EntityId, c.CreatedAtUtc });

        builder.HasIndex(c => new { c.TenantId, c.AuthorUserId });
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(n => n.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(n => n.ReadAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(n => n.Type).HasMaxLength(32).IsRequired();
        builder.Property(n => n.EntityType).HasMaxLength(32).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(1000).IsRequired();

        builder.HasIndex(n => new { n.TenantId, n.RecipientUserId, n.IsRead, n.CreatedAtUtc })
            .HasDatabaseName("ix_notifications_recipient_unread_recent");

        builder.HasIndex(x => new { x.TenantId, x.RecipientUserId, x.EntityType, x.EntityId, x.Type })
            .IsUnique()
            .HasDatabaseName("UX_Notifications_Tenant_Recipient_Entity_Type");
    }
}
