using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAlign.Infrastructure.Persistence.Configurations;

public class GlassProjectConfiguration : IEntityTypeConfiguration<GlassProject>
{
    public void Configure(EntityTypeBuilder<GlassProject> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.ProjectName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.SiteAddressLine1).HasMaxLength(200);
        builder.Property(p => p.SiteAddressLine2).HasMaxLength(200);
        builder.Property(p => p.SiteCity).HasMaxLength(100);
        builder.Property(p => p.SiteDistrict).HasMaxLength(100);
        builder.Property(p => p.SitePostalCode).HasMaxLength(20);
        builder.Property(p => p.SiteCountryCode).HasMaxLength(3);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.FireSafetyClass).HasMaxLength(50);
        builder.Property(p => p.BuildingHeightM).HasColumnType("numeric(10,2)");
        builder.Property(p => p.TotalAreaM2).HasColumnType("numeric(12,3)");
        builder.Property(p => p.Subtotal).HasColumnType("numeric(18,4)");
        builder.Property(p => p.DiscountTotal).HasColumnType("numeric(18,4)");
        builder.Property(p => p.TaxTotal).HasColumnType("numeric(18,4)");
        builder.Property(p => p.GrandTotal).HasColumnType("numeric(18,4)");
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.FxRateToBase).HasColumnType("numeric(18,6)");
        builder.Property(p => p.WindLoadPaCalculated).HasColumnType("numeric(10,2)");
        builder.Property(p => p.WeightedUValue).HasColumnType("numeric(6,3)");
        builder.Property(p => p.WeightedSoundDb).HasColumnType("numeric(6,2)");
        builder.Property(p => p.Notes).HasMaxLength(4000);
        builder.Property(p => p.ValidUntilDate).HasColumnType("timestamp with time zone");
        builder.Property(p => p.FxRateLockedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(p => p.EnclosureCategory).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.EnclosureSubtype).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.GeometryMode).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.MountingTopology).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.RoofPitchDeg).HasColumnType("numeric(5,2)");
        builder.Property(p => p.CurtainWallCassetteSpecJson).HasMaxLength(4000);
        builder.Property(p => p.PolygonVerticesJson).HasMaxLength(8000);
        builder.Property(p => p.MetadataJson).HasMaxLength(4000);

        builder.Property(p => p.BomStaleReason).HasMaxLength(32);
        builder.Property(p => p.StaleSinceUtc).HasColumnType("timestamp with time zone");

        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken();

        builder.HasMany(p => p.Runs)
            .WithOne()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Connections)
            .WithOne()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TenantId, p.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => new { p.TenantId, p.CustomerId });
        builder.HasIndex(p => new { p.TenantId, p.Status });
        builder.HasIndex(p => new { p.TenantId, p.AssignedDesignerUserId });
        builder.HasIndex(p => new { p.TenantId, p.UpdatedAtUtc });
        builder.HasIndex(p => new { p.TenantId, p.EnclosureCategory, p.EnclosureSubtype })
            .HasDatabaseName("IX_GlassProjects_TenantId_Category_Subtype");
        builder.HasIndex(p => new { p.TenantId, p.IsBomStale })
            .HasFilter("is_bom_stale = true")
            .HasDatabaseName("ix_glass_projects_stale");
    }
}

public class GlassProjectRunConfiguration : IEntityTypeConfiguration<GlassProjectRun>
{
    public void Configure(EntityTypeBuilder<GlassProjectRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Label).HasMaxLength(100).IsRequired();
        builder.Property(r => r.OriginX).HasColumnType("numeric(12,3)");
        builder.Property(r => r.OriginY).HasColumnType("numeric(12,3)");
        builder.Property(r => r.RotationDeg).HasColumnType("numeric(7,3)");
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(r => r.GeomTiltDeg).HasColumnType("numeric(5,2)");
        builder.Property(r => r.GeomArcSweepDeg).HasColumnType("numeric(5,2)");
        builder.Property(r => r.ArcGlassBent).HasDefaultValue(false);
        builder.Property(r => r.ConcurrencyToken).IsConcurrencyToken();

        builder.HasMany(r => r.Panels)
            .WithOne()
            .HasForeignKey(p => p.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.ProjectId, r.OrderIndex });
    }
}

public class RunConnectionConfiguration : IEntityTypeConfiguration<RunConnection>
{
    public void Configure(EntityTypeBuilder<RunConnection> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.JointAngleDeg).HasColumnType("numeric(7,3)");
        builder.Property(c => c.MitreCutDeg).HasColumnType("numeric(7,3)");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(c => new { c.TenantId, c.ProjectId });
        builder.HasIndex(c => new { c.TenantId, c.RunAId, c.RunBId }).IsUnique();
    }
}

public class GlassProjectPanelConfiguration : IEntityTypeConfiguration<GlassProjectPanel>
{
    public void Configure(EntityTypeBuilder<GlassProjectPanel> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.OpeningType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.PanelKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.TopShape).HasMaxLength(16);
        builder.Property(p => p.ShapeKind).HasMaxLength(16);
        builder.Property(p => p.ShapePointsJson).HasMaxLength(8000);
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(p => new { p.TenantId, p.RunId, p.PanelIndex });

        builder.HasMany(p => p.Hardware)
            .WithOne()
            .HasForeignKey(h => h.PanelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GlassProjectPanelHardwareConfiguration : IEntityTypeConfiguration<GlassProjectPanelHardware>
{
    public void Configure(EntityTypeBuilder<GlassProjectPanelHardware> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Quantity).HasColumnType("numeric(12,3)");
        builder.Property(h => h.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(h => h.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasOne<HardwareItem>()
            .WithMany()
            .HasForeignKey(h => h.HardwareItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(h => new { h.TenantId, h.PanelId });
        builder.HasIndex(h => h.HardwareItemId);
    }
}

public class GlassProjectSceneConfiguration : IEntityTypeConfiguration<GlassProjectScene>
{
    public void Configure(EntityTypeBuilder<GlassProjectScene> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Label).HasMaxLength(200);
        builder.Property(s => s.SceneJsonCompressed).HasColumnType("bytea").IsRequired();
        builder.Property(s => s.ThumbnailUrl).HasMaxLength(500);
        builder.Property(s => s.CameraStateJson).HasColumnType("text");
        builder.Property(s => s.ApprovalSignatureUrl).HasMaxLength(500);
        builder.Property(s => s.SavedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(s => new { s.TenantId, s.ProjectId, s.Version }).IsUnique();
    }
}

public class GlassProjectChangeLogConfiguration : IEntityTypeConfiguration<GlassProjectChangeLog>
{
    public void Configure(EntityTypeBuilder<GlassProjectChangeLog> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ChangeKind).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.ChangeSummary).HasMaxLength(500).IsRequired();
        builder.Property(c => c.ChangeDiffJson).HasColumnType("jsonb");
        builder.Property(c => c.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(c => c.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(c => new { c.TenantId, c.ProjectId, c.CreatedAtUtc });
    }
}

public class GlassProjectAttachmentConfiguration : IEntityTypeConfiguration<GlassProjectAttachment>
{
    public void Configure(EntityTypeBuilder<GlassProjectAttachment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Url).HasMaxLength(500).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100);
        builder.Property(a => a.Caption).HasMaxLength(500);
        builder.Property(a => a.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(a => a.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(a => new { a.TenantId, a.ProjectId, a.Kind });
    }
}

public class GlassProjectBOMLineConfiguration : IEntityTypeConfiguration<GlassProjectBOMLine>
{
    public void Configure(EntityTypeBuilder<GlassProjectBOMLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Description).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Quantity).HasColumnType("numeric(18,4)");
        builder.Property(l => l.Unit).HasMaxLength(20).IsRequired();
        builder.Property(l => l.UnitCost).HasColumnType("numeric(18,4)");
        builder.Property(l => l.UnitPriceOverride).HasColumnType("numeric(18,4)");
        builder.Property(l => l.LineCost).HasColumnType("numeric(18,4)");
        builder.Property(l => l.Currency).HasMaxLength(3).IsRequired();
        builder.Property(l => l.Source).HasMaxLength(200);
        builder.Property(l => l.ProductId).IsRequired(false);
        builder.Property(l => l.IsService).HasDefaultValue(false);
        builder.Property(l => l.IsManual).HasDefaultValue(false);
        builder.Property(l => l.CutSpecJson).HasMaxLength(8000);
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.TenantId, l.ProjectId, l.Kind });
        builder.HasIndex(l => new { l.TenantId, l.ProductId })
            .HasFilter("product_id IS NOT NULL");
    }
}

public class GlassProjectCuttingPlanConfiguration : IEntityTypeConfiguration<GlassProjectCuttingPlan>
{
    public void Configure(EntityTypeBuilder<GlassProjectCuttingPlan> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PlanType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.PlanJson).HasColumnType("jsonb");
        builder.Property(p => p.TotalWasteMm2).HasColumnType("numeric(18,2)");
        builder.Property(p => p.TotalWasteMm).HasColumnType("numeric(18,2)");
        builder.Property(p => p.UtilizationPercent).HasColumnType("numeric(6,3)");
        builder.Property(p => p.GeneratedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(p => new { p.TenantId, p.ProjectId, p.PlanType, p.GeneratedAtUtc });
    }
}

public class GlassProjectQuoteSnapshotConfiguration : IEntityTypeConfiguration<GlassProjectQuoteSnapshot>
{
    public void Configure(EntityTypeBuilder<GlassProjectQuoteSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PdfUrl).HasMaxLength(500).IsRequired();
        builder.Property(s => s.GrandTotal).HasColumnType("numeric(18,4)");
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.IssuedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.ValidUntilUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(s => new { s.TenantId, s.ProjectId, s.IssuedAtUtc });
    }
}

public class GlassProjectShareTokenConfiguration : IEntityTypeConfiguration<GlassProjectShareToken>
{
    public void Configure(EntityTypeBuilder<GlassProjectShareToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Token).HasMaxLength(64).IsRequired();
        builder.Property(t => t.RejectionReason).HasMaxLength(1000);
        builder.Property(t => t.SignatureImageUrl).HasMaxLength(500);
        builder.Property(t => t.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.LastViewedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.AcceptedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.RejectedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.ProjectId });
    }
}

public class FieldSurveyConfiguration : IEntityTypeConfiguration<FieldSurvey>
{
    public void Configure(EntityTypeBuilder<FieldSurvey> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.GpsLat).HasColumnType("numeric(10,7)");
        builder.Property(s => s.GpsLng).HasColumnType("numeric(10,7)");
        builder.Property(s => s.BuildingHeightM).HasColumnType("numeric(10,2)");
        builder.Property(s => s.SlopeTopMm).HasColumnType("numeric(10,2)");
        builder.Property(s => s.SlopeBottomMm).HasColumnType("numeric(10,2)");
        builder.Property(s => s.SlopeLeftMm).HasColumnType("numeric(10,2)");
        builder.Property(s => s.SlopeRightMm).HasColumnType("numeric(10,2)");
        builder.Property(s => s.RawMeasurementsJson).HasColumnType("jsonb");
        builder.Property(s => s.ObstaclesJson).HasColumnType("jsonb");
        builder.Property(s => s.PhotoUrlsJson).HasColumnType("jsonb");
        builder.Property(s => s.AnnotatedPhotoUrlsJson).HasColumnType("jsonb");
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.SurveyedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.AppliedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(s => s.ConcurrencyToken).IsConcurrencyToken();

        builder.HasIndex(s => new { s.TenantId, s.ProjectId, s.Status });
    }
}

public class GlassWorkOrderConfiguration : IEntityTypeConfiguration<GlassWorkOrder>
{
    public void Configure(EntityTypeBuilder<GlassWorkOrder> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.WorkloadM2).HasColumnType("numeric(12,3)");
        builder.Property(w => w.ChecklistsJson).HasColumnType("jsonb");
        builder.Property(w => w.DefectNotes).HasMaxLength(2000);
        builder.Property(w => w.ScheduledStartDate).HasColumnType("timestamp with time zone");
        builder.Property(w => w.ScheduledEndDate).HasColumnType("timestamp with time zone");
        builder.Property(w => w.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(w => w.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.Property(w => w.BomSnapshotJson).HasMaxLength(64000);
        builder.Property(w => w.BomSnapshotTotal).HasColumnType("numeric(18,4)");
        builder.Property(w => w.ConcurrencyToken).IsConcurrencyToken();

        builder.Property(w => w.RevisionCountAtLastDefect).HasDefaultValue(0);

        builder.HasIndex(w => new { w.TenantId, w.ProjectId });
        builder.HasIndex(w => new { w.TenantId, w.Status, w.ScheduledStartDate });
        builder.HasIndex(w => w.TenantId)
            .HasFilter("bom_snapshot_json IS NOT NULL")
            .HasDatabaseName("ix_work_orders_with_snapshot");
    }
}

public class GlassWorkOrderRevisionConfiguration : IEntityTypeConfiguration<GlassWorkOrderRevision>
{
    public void Configure(EntityTypeBuilder<GlassWorkOrderRevision> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(r => r.PreviousSnapshotJson).HasMaxLength(64000);
        builder.Property(r => r.NewSnapshotJson).HasMaxLength(64000).IsRequired();
        builder.Property(r => r.DeltaJson).HasMaxLength(16000);
        builder.Property(r => r.DeltaPercent).HasColumnType("numeric(6,2)");
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.RejectionReason).HasMaxLength(500);
        builder.Property(r => r.ConcurrencyToken).IsConcurrencyToken();
        builder.Property(r => r.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(r => r.ApprovedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(r => new { r.WorkOrderId, r.RevisionNumber }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.Status });
    }
}

public class GlassProjectOrderLinkConfiguration : IEntityTypeConfiguration<GlassProjectOrderLink>
{
    public void Configure(EntityTypeBuilder<GlassProjectOrderLink> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.LinkedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(l => new { l.TenantId, l.ProjectId }).IsUnique();
        builder.HasIndex(l => new { l.TenantId, l.OrderId }).IsUnique();
    }
}

public class GlassNotificationLogConfiguration : IEntityTypeConfiguration<GlassNotificationLog>
{
    public void Configure(EntityTypeBuilder<GlassNotificationLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.EventCode).HasConversion<string>().HasMaxLength(40);
        builder.Property(l => l.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.RecipientKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.RecipientAddress).HasMaxLength(255).IsRequired();
        builder.Property(l => l.PayloadJson).HasColumnType("jsonb");
        builder.Property(l => l.ProviderMessageId).HasMaxLength(200);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.ErrorMessage).HasMaxLength(2000);
        builder.Property(l => l.DeliveredAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.ReadAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(l => l.UpdatedAtUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(l => new { l.TenantId, l.ProjectId });
        builder.HasIndex(l => new { l.TenantId, l.Status });
    }
}
