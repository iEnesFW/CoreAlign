using CoreAlign.Domain.Entities.GlassEnclosure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class ProjectTemplateConfiguration : IEntityTypeConfiguration<ProjectTemplate>
{
    public void Configure(EntityTypeBuilder<ProjectTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(64).IsRequired();
        builder.Property(t => t.DisplayNameKey).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(t => t.Subtype).HasConversion<string>().HasMaxLength(40);
        builder.Property(t => t.GeometryMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.MountingTopology).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.DefaultConnectorKind).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.RoofPitchDeg).HasColumnType("numeric(6,2)");
        builder.Property(t => t.ThumbnailUrl).HasMaxLength(500);
        builder.Property(t => t.DescriptionKey).HasMaxLength(150);
        builder.Property(t => t.MetadataJson).HasColumnType("jsonb");
        builder.Property(t => t.Visibility).HasConversion<string>().HasMaxLength(40);
        builder.Property(t => t.RejectionReason).HasMaxLength(1000);
        builder.Property(t => t.AverageRating).HasColumnType("numeric(3,2)");
        builder.Property(t => t.SubmittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.PublishedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasMany(t => t.RunPresets)
            .WithOne()
            .HasForeignKey(p => p.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.Category, t.IsActive });
        builder.HasIndex(t => t.IsSystemTemplate);
        builder.HasIndex(t => t.Visibility);
        builder.HasIndex(t => new { t.Visibility, t.Category, t.IsActive });
        builder.HasIndex(t => new { t.Visibility, t.DownloadCount });
    }
}

public class ProjectTemplateReviewConfiguration : IEntityTypeConfiguration<ProjectTemplateReview>
{
    public void Configure(EntityTypeBuilder<ProjectTemplateReview> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.CommentMd).HasMaxLength(4000);
        builder.Property(r => r.ReviewedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(r => new { r.TemplateId, r.TenantId, r.ReviewerUserId }).IsUnique();
        builder.HasIndex(r => r.TemplateId);
        builder.HasIndex(r => new { r.TemplateId, r.ReviewedAtUtc });
    }
}

public class ProjectTemplateInstallConfiguration : IEntityTypeConfiguration<ProjectTemplateInstall>
{
    public void Configure(EntityTypeBuilder<ProjectTemplateInstall> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InstalledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(i => i.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(i => new { i.MarketplaceTemplateId, i.TenantId });
        builder.HasIndex(i => new { i.TenantId, i.InstalledAtUtc });
    }
}

public class ProjectTemplateRunPresetConfiguration : IEntityTypeConfiguration<ProjectTemplateRunPreset>
{
    public void Configure(EntityTypeBuilder<ProjectTemplateRunPreset> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.LabelKey).HasMaxLength(150).IsRequired();
        builder.Property(p => p.DefaultOpeningType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.OriginX).HasColumnType("numeric(12,3)");
        builder.Property(p => p.OriginY).HasColumnType("numeric(12,3)");
        builder.Property(p => p.RotationDeg).HasColumnType("numeric(6,2)");
        builder.Property(p => p.CornerJointAngleDeg).HasColumnType("numeric(6,2)");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.TemplateId, p.OrderIndex });
    }
}
