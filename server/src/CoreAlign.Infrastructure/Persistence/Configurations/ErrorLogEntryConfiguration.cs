using CoreAlign.Domain.Entities.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ErrorLogEntryConfiguration : IEntityTypeConfiguration<ErrorLogEntry>
{
    public void Configure(EntityTypeBuilder<ErrorLogEntry> builder)
    {
        builder.ToTable("error_logs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CorrelationId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.TraceId).HasMaxLength(128);
        builder.Property(e => e.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.Source).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.HttpMethod).HasMaxLength(8);
        builder.Property(e => e.Path).HasMaxLength(512);
        builder.Property(e => e.ExceptionType).HasMaxLength(256);
        builder.Property(e => e.Message).HasMaxLength(8000).IsRequired();
        builder.Property(e => e.StackTrace);
        builder.Property(e => e.UserName).HasMaxLength(256);
        builder.Property(e => e.ClientPage).HasMaxLength(512);
        builder.Property(e => e.ClientComponent).HasMaxLength(256);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
        builder.Property(e => e.ContextJson);
        builder.Property(e => e.ResolutionNotes).HasMaxLength(2000);
        builder.Property(e => e.ResolvedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(e => e.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => e.CorrelationId);
        builder.HasIndex(e => new { e.TenantId, e.OccurredAtUtc });
        builder.HasIndex(e => new { e.Severity, e.OccurredAtUtc });
        builder.HasIndex(e => e.IsResolved);
    }
}
