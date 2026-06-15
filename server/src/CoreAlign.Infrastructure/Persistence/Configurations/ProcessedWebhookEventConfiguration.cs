using CoreAlign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Gateway).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EventId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ProcessedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(e => new { e.Gateway, e.EventId, e.EventType })
            .IsUnique()
            .HasDatabaseName("ix_processed_webhook_events_gateway_eventid_eventtype");
    }
}
