using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class ProjectTemplate : TenantEntity, IHasConcurrencyToken, IGlobalReadable
{
    public string Code { get; private set; } = string.Empty;
    public string DisplayNameKey { get; private set; } = string.Empty;
    public bool IsSystemTemplate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public EnclosureCategory Category { get; private set; }
    public EnclosureSubtype Subtype { get; private set; }
    public GeometryMode GeometryMode { get; private set; }
    public MountingTopology MountingTopology { get; private set; }
    public ConnectorKind DefaultConnectorKind { get; private set; }
    public decimal? RoofPitchDeg { get; private set; }
    public int? RidgeHeightMm { get; private set; }
    public int? EaveHeightMm { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? DescriptionKey { get; private set; }
    public string? MetadataJson { get; private set; }
    public int SortOrder { get; private set; }
    public ProjectTemplateVisibility Visibility { get; private set; } = ProjectTemplateVisibility.TenantOnly;
    public Guid? SubmittedByTenantId { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public Guid? PublishedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public int DownloadCount { get; private set; }
    public decimal? AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    private readonly List<ProjectTemplateRunPreset> _runPresets = new();
    public IReadOnlyCollection<ProjectTemplateRunPreset> RunPresets => _runPresets;

    protected ProjectTemplate() { }

    public ProjectTemplate(
        string code,
        string displayNameKey,
        bool isSystemTemplate,
        EnclosureCategory category,
        EnclosureSubtype subtype,
        GeometryMode geometryMode,
        MountingTopology mountingTopology,
        ConnectorKind defaultConnectorKind,
        decimal? roofPitchDeg = null,
        int? ridgeHeightMm = null,
        int? eaveHeightMm = null,
        string? thumbnailUrl = null,
        string? descriptionKey = null,
        string? metadataJson = null,
        int sortOrder = 0)
    {
        Code = code;
        DisplayNameKey = displayNameKey;
        IsSystemTemplate = isSystemTemplate;
        Category = category;
        Subtype = subtype;
        GeometryMode = geometryMode;
        MountingTopology = mountingTopology;
        DefaultConnectorKind = defaultConnectorKind;
        RoofPitchDeg = roofPitchDeg;
        RidgeHeightMm = ridgeHeightMm;
        EaveHeightMm = eaveHeightMm;
        ThumbnailUrl = thumbnailUrl;
        DescriptionKey = descriptionKey;
        MetadataJson = metadataJson;
        SortOrder = sortOrder;
    }

    public void AddRunPreset(ProjectTemplateRunPreset preset) => _runPresets.Add(preset);

    public void UpdateDefinition(
        string displayNameKey,
        EnclosureCategory category,
        EnclosureSubtype subtype,
        GeometryMode geometryMode,
        MountingTopology mountingTopology,
        ConnectorKind defaultConnectorKind,
        decimal? roofPitchDeg,
        int? ridgeHeightMm,
        int? eaveHeightMm,
        string? thumbnailUrl,
        string? descriptionKey,
        string? metadataJson,
        int sortOrder)
    {
        DisplayNameKey = displayNameKey;
        Category = category;
        Subtype = subtype;
        GeometryMode = geometryMode;
        MountingTopology = mountingTopology;
        DefaultConnectorKind = defaultConnectorKind;
        RoofPitchDeg = roofPitchDeg;
        RidgeHeightMm = ridgeHeightMm;
        EaveHeightMm = eaveHeightMm;
        ThumbnailUrl = thumbnailUrl;
        DescriptionKey = descriptionKey;
        MetadataJson = metadataJson;
        SortOrder = sortOrder;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceRunPresets(IEnumerable<ProjectTemplateRunPreset> presets)
    {
        _runPresets.Clear();
        _runPresets.AddRange(presets);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SubmitToMarketplace(Guid submitterTenantId)
    {
        if (Visibility != ProjectTemplateVisibility.TenantOnly)
        {
            throw new InvalidOperationException("GlassEnclosure.Marketplace.OnlyTenantOnlyCanBeSubmitted");
        }
        Visibility = ProjectTemplateVisibility.MarketplaceSubmitted;
        SubmittedByTenantId = submitterTenantId;
        SubmittedAtUtc = DateTime.UtcNow;
        RejectionReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Publish(Guid publisherUserId)
    {
        if (Visibility != ProjectTemplateVisibility.MarketplaceSubmitted)
        {
            throw new InvalidOperationException("GlassEnclosure.Marketplace.OnlySubmittedCanBePublished");
        }
        Visibility = ProjectTemplateVisibility.MarketplacePublished;
        PublishedAtUtc = DateTime.UtcNow;
        PublishedByUserId = publisherUserId;
        RejectionReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Visibility != ProjectTemplateVisibility.MarketplaceSubmitted)
        {
            throw new InvalidOperationException("GlassEnclosure.Marketplace.OnlySubmittedCanBeRejected");
        }
        Visibility = ProjectTemplateVisibility.MarketplaceRejected;
        RejectionReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void IncrementDownload()
    {
        DownloadCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecalculateRating(int newReviewCount, decimal? newAverage)
    {
        ReviewCount = newReviewCount;
        AverageRating = newAverage;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsCloneOf(Guid sourceTemplateId, Guid newTenantId)
    {
        TenantId = newTenantId;
        Visibility = ProjectTemplateVisibility.TenantOnly;
        SubmittedByTenantId = null;
        SubmittedAtUtc = null;
        PublishedAtUtc = null;
        PublishedByUserId = null;
        RejectionReason = null;
        DownloadCount = 0;
        AverageRating = null;
        ReviewCount = 0;
        IsSystemTemplate = false;
        MetadataJson = string.IsNullOrWhiteSpace(MetadataJson)
            ? $"{{\"sourceTemplateId\":\"{sourceTemplateId}\"}}"
            : MetadataJson;
    }
}
