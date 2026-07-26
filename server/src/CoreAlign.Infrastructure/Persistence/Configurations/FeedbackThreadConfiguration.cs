using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class FeedbackTicketConfiguration : IEntityTypeConfiguration<FeedbackTicket>
{
    public void Configure(EntityTypeBuilder<FeedbackTicket> builder)
    {
        builder.Property(f => f.ConcurrencyToken).IsConcurrencyToken().HasDefaultValue(0L);
        builder.Property(f => f.StatusChangeCount).HasDefaultValue(0);
        builder.Property(f => f.Title).HasMaxLength(200);
        builder.Property(f => f.Module).HasMaxLength(100);
        builder.Property(f => f.PageUrl).HasMaxLength(500);
        builder.Property(f => f.CreatedByName).HasMaxLength(200);
        builder.HasIndex(f => new { f.TenantId, f.Status, f.CreatedAtUtc });
    }
}

public class FeedbackTicketCommentConfiguration : IEntityTypeConfiguration<FeedbackTicketComment>
{
    public void Configure(EntityTypeBuilder<FeedbackTicketComment> builder)
    {
        // DbSet-less entity (accessed via _context.Set<FeedbackTicketComment>()); the explicit plural
        // table name keeps the model in step with the hand-authored migration — the EF default for a
        // DbSet-less type is the SINGULAR snake_case name.
        builder.ToTable("feedback_ticket_comments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body).HasMaxLength(FeedbackTicketComment.MaxBodyLength).IsRequired();
        builder.Property(c => c.AuthorName).HasMaxLength(200);
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder
            .HasOne<FeedbackTicket>()
            .WithMany()
            .HasForeignKey(c => c.FeedbackTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TenantId, c.FeedbackTicketId, c.CreatedAtUtc });
    }
}

public class FeedbackAttachmentConfiguration : IEntityTypeConfiguration<FeedbackAttachment>
{
    public void Configure(EntityTypeBuilder<FeedbackAttachment> builder)
    {
        builder.ToTable("feedback_attachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.DisplayFileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder
            .HasOne<FeedbackTicket>()
            .WithMany()
            .HasForeignKey(a => a.FeedbackTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.TenantId, a.FeedbackTicketId, a.DisplayOrder });
    }
}
