using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.AiHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public sealed class AiKbDocumentConfiguration : IEntityTypeConfiguration<AiKbDocument>
{
    public void Configure(EntityTypeBuilder<AiKbDocument> builder)
    {
        builder.ToTable("ai_kb_documents", table =>
        {
            table.HasCheckConstraint(
                "ck_ai_kb_documents_source_type",
                "source_type IN ('Route','I18n','ModuleDoc','Article','Sector','SourceCode')");
            table.HasCheckConstraint(
                "ck_ai_kb_documents_scope",
                "scope IN ('Public','Tenant','Role')");
            table.HasCheckConstraint(
                "ck_ai_kb_documents_role_requires_role",
                "scope <> 'Role' OR required_role IS NOT NULL");
        });
        builder.HasKey(d => d.Id);

        builder.Property(d => d.SourceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.SourceRef).HasMaxLength(512).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(512).IsRequired();
        builder.Property(d => d.Locale).HasMaxLength(10).IsRequired();
        builder.Property(d => d.Scope).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.RequiredRole).HasMaxLength(64);
        builder.Property(d => d.ContentHash).HasMaxLength(64).IsRequired();

        builder.HasIndex(d => new { d.SourceType, d.SourceRef, d.Locale }).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Chunks)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AiKbChunkConfiguration : IEntityTypeConfiguration<AiKbChunk>
{
    public void Configure(EntityTypeBuilder<AiKbChunk> builder)
    {
        builder.ToTable("ai_kb_chunks", table =>
        {
            table.HasCheckConstraint(
                "ck_ai_kb_chunks_scope",
                "scope IN ('Public','Tenant','Role')");
            table.HasCheckConstraint(
                "ck_ai_kb_chunks_role_requires_role",
                "scope <> 'Role' OR required_role IS NOT NULL");
        });
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.Embedding).IsRequired();
        builder.Property(c => c.Locale).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Scope).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.RequiredRole).HasMaxLength(64);

        builder.HasIndex(c => new { c.Locale, c.Scope, c.TenantId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AiHelperQueryLogConfiguration : IEntityTypeConfiguration<AiHelperQueryLog>
{
    public void Configure(EntityTypeBuilder<AiHelperQueryLog> builder)
    {
        builder.ToTable("ai_helper_query_logs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Question).HasMaxLength(2000).IsRequired();
        builder.Property(l => l.Locale).HasMaxLength(10).IsRequired();
        builder.Property(l => l.RoutePath).HasMaxLength(512);
        builder.Property(l => l.ChatModel).HasMaxLength(64).IsRequired();
        builder.Property(l => l.TopScore).HasPrecision(9, 6);
        builder.Property(l => l.RetrievedJson).IsRequired();
        builder.Property(l => l.AnswerText).IsRequired();

        builder.HasIndex(l => l.CreatedAtUtc);
        builder.HasIndex(l => l.ConversationId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AiHelperFeedbackConfiguration : IEntityTypeConfiguration<AiHelperFeedback>
{
    public void Configure(EntityTypeBuilder<AiHelperFeedback> builder)
    {
        builder.ToTable("ai_helper_feedback");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Reason).HasMaxLength(1000);

        // WHY: AnswerId is a soft reference to a best-effort query-log row (never-throws writer); no hard FK so feedback is never rejected when the trace is absent.
        builder.HasIndex(f => f.AnswerId);
        builder.HasIndex(f => f.CreatedAtUtc);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
