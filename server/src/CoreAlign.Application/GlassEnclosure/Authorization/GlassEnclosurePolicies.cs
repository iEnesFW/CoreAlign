namespace CoreAlign.Application.GlassEnclosure.Authorization;

public static class GlassEnclosureRoles
{
    public const string TenantAdmin = "TenantAdmin";
    public const string Salesperson = "GlassEnclosure.Salesperson";
    public const string Designer = "GlassEnclosure.Designer";
    public const string Approver = "GlassEnclosure.Approver";
    public const string Producer = "GlassEnclosure.Producer";
    public const string Installer = "GlassEnclosure.Installer";
    public const string Surveyor = "GlassEnclosure.Surveyor";

    public static readonly string[] AllRoles =
    {
        TenantAdmin, Salesperson, Designer, Approver, Producer, Installer, Surveyor,
    };
}

public static class GlassEnclosurePolicies
{
    public const string ProjectView = "GlassEnclosure.Project.View";
    public const string ProjectViewAll = "GlassEnclosure.Project.ViewAll";
    public const string ProjectCreate = "GlassEnclosure.Project.Create";
    public const string ProjectUpdate = "GlassEnclosure.Project.Update";
    public const string ProjectDelete = "GlassEnclosure.Project.Delete";
    public const string ProjectClone = "GlassEnclosure.Project.Clone";

    public const string DesignerOpen = "GlassEnclosure.Designer.Open";
    public const string DesignerPriceVisible = "GlassEnclosure.Designer.PriceVisible";
    public const string DesignerPriceEdit = "GlassEnclosure.Designer.PriceEdit";
    public const string DesignerDiscountApply = "GlassEnclosure.Designer.DiscountApply";
    public const string DesignerDiscountOverride = "GlassEnclosure.Designer.DiscountOverride";

    public const string QuoteGenerate = "GlassEnclosure.Quote.Generate";
    public const string QuoteSend = "GlassEnclosure.Quote.Send";
    public const string QuoteAccept = "GlassEnclosure.Quote.Accept";

    public const string OrderConvert = "GlassEnclosure.Order.Convert";

    public const string ProductionRelease = "GlassEnclosure.Production.Release";
    public const string ProductionSchedule = "GlassEnclosure.Production.Schedule";
    public const string ProductionUpdateStatus = "GlassEnclosure.Production.UpdateStatus";
    public const string ProductionRecordDefect = "GlassEnclosure.Production.RecordDefect";

    public const string WorkOrderRevisionApprove = "GlassEnclosure.WorkOrderRevision.Approve";
    public const string WorkOrderRevisionReject = "GlassEnclosure.WorkOrderRevision.Reject";

    public const string CuttingReportDownload = "GlassEnclosure.CuttingReport.Download";

    public const string InstallationUpdateStatus = "GlassEnclosure.Installation.UpdateStatus";
    public const string InstallationCompleteChecklist = "GlassEnclosure.Installation.CompleteChecklist";

    public const string FieldSurveyCreate = "GlassEnclosure.FieldSurvey.Create";
    public const string FieldSurveySubmit = "GlassEnclosure.FieldSurvey.Submit";
    public const string FieldSurveyApprove = "GlassEnclosure.FieldSurvey.Approve";

    public const string CatalogView = "GlassEnclosure.Catalog.View";
    public const string CatalogUpdate = "GlassEnclosure.Catalog.Update";
    public const string CatalogImport = "GlassEnclosure.Catalog.Import";

    public const string DiscountRuleUpdate = "GlassEnclosure.DiscountRule.Update";
    public const string NotificationTemplateUpdate = "GlassEnclosure.NotificationTemplate.Update";
    public const string BrandVendorUpdate = "GlassEnclosure.BrandVendor.Update";

    public const string SettingsUpdate = "GlassEnclosure.Settings.Update";
    public const string SettingsWindZone = "GlassEnclosure.Settings.WindZone";

    public const string Anonymize = "GlassEnclosure.Anonymize";
    public const string ExportProjectData = "GlassEnclosure.Export.ProjectData";
    public const string ReportsView = "GlassEnclosure.Reports.View";

    public const string MarketplaceBrowse = "GlassEnclosure.Marketplace.Browse";
    public const string MarketplaceSubmit = "GlassEnclosure.Marketplace.Submit";
    public const string MarketplaceInstall = "GlassEnclosure.Marketplace.Install";
    public const string MarketplaceReview = "GlassEnclosure.Marketplace.Review";
    public const string MarketplaceAdmin = "GlassEnclosure.Marketplace.Admin";

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> PolicyRoleMap { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [ProjectView] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Salesperson, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver, GlassEnclosureRoles.Producer, GlassEnclosureRoles.Installer, GlassEnclosureRoles.Surveyor },
            [ProjectViewAll] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver, GlassEnclosureRoles.Producer },
            [ProjectCreate] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Salesperson, GlassEnclosureRoles.Designer },
            [ProjectUpdate] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver },
            [ProjectDelete] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },
            [ProjectClone] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Salesperson, GlassEnclosureRoles.Designer },

            [DesignerOpen] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver },
            [DesignerPriceVisible] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver, GlassEnclosureRoles.Salesperson },
            [DesignerPriceEdit] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },
            [DesignerDiscountApply] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver, GlassEnclosureRoles.Salesperson },
            [DesignerDiscountOverride] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },

            [QuoteGenerate] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Salesperson, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver },
            [QuoteSend] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Salesperson, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver },
            [QuoteAccept] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },

            [OrderConvert] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },

            [ProductionRelease] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver, GlassEnclosureRoles.Producer },
            [ProductionSchedule] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver, GlassEnclosureRoles.Producer },
            [ProductionUpdateStatus] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Producer },
            [ProductionRecordDefect] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Producer, GlassEnclosureRoles.Installer },

            [WorkOrderRevisionApprove] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },
            [WorkOrderRevisionReject] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },

            [CuttingReportDownload] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver, GlassEnclosureRoles.Producer },

            [InstallationUpdateStatus] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Installer },
            [InstallationCompleteChecklist] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Installer },

            [FieldSurveyCreate] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Surveyor },
            [FieldSurveySubmit] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Surveyor },
            [FieldSurveyApprove] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Approver },

            [CatalogView] = GlassEnclosureRoles.AllRoles,
            [CatalogUpdate] = new[] { GlassEnclosureRoles.TenantAdmin },
            [CatalogImport] = new[] { GlassEnclosureRoles.TenantAdmin },

            [DiscountRuleUpdate] = new[] { GlassEnclosureRoles.TenantAdmin },
            [NotificationTemplateUpdate] = new[] { GlassEnclosureRoles.TenantAdmin },
            [BrandVendorUpdate] = new[] { GlassEnclosureRoles.TenantAdmin },

            [SettingsUpdate] = new[] { GlassEnclosureRoles.TenantAdmin },
            [SettingsWindZone] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },

            [Anonymize] = new[] { GlassEnclosureRoles.TenantAdmin },
            [ExportProjectData] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver },
            [ReportsView] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Approver, GlassEnclosureRoles.Producer, GlassEnclosureRoles.Salesperson },

            [MarketplaceBrowse] = GlassEnclosureRoles.AllRoles,
            [MarketplaceSubmit] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer },
            [MarketplaceInstall] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Salesperson },
            [MarketplaceReview] = new[] { GlassEnclosureRoles.TenantAdmin, GlassEnclosureRoles.Designer, GlassEnclosureRoles.Salesperson, GlassEnclosureRoles.Approver },
            [MarketplaceAdmin] = new[] { GlassEnclosureRoles.TenantAdmin },
        };
}
