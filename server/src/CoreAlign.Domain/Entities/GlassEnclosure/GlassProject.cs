using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProject : TenantEntity, IHasConcurrencyToken, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        ((ISoftDeletable)this).MarkDeletedInternal(userId, reason, utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void Restore()
    {
        ((ISoftDeletable)this).RestoreInternal();
        UpdatedAtUtc = DateTime.UtcNow;
    }


    public string Code { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string ProjectName { get; private set; } = string.Empty;
    public string? SiteAddressLine1 { get; private set; }
    public string? SiteAddressLine2 { get; private set; }
    public string? SiteCity { get; private set; }
    public string? SiteDistrict { get; private set; }
    public string? SitePostalCode { get; private set; }
    public string? SiteCountryCode { get; private set; }
    public GlassProjectStatus Status { get; private set; } = GlassProjectStatus.Draft;
    public Guid CreatedByUserId { get; private set; }
    public Guid? AssignedDesignerUserId { get; private set; }
    public Guid? AssignedSalespersonUserId { get; private set; }
    public int? FloorNumber { get; private set; }
    public decimal? BuildingHeightM { get; private set; }
    public Guid? WindZoneId { get; private set; }
    public Guid? ClimateZoneId { get; private set; }
    public string? FireSafetyClass { get; private set; }
    public bool ScaffoldingRequired { get; private set; }
    public bool CraneRequired { get; private set; }
    public decimal TotalAreaM2 { get; private set; }
    public int TotalPanels { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal FxRateToBase { get; private set; } = 1m;
    public DateTime? FxRateLockedAtUtc { get; private set; }
    public decimal? WindLoadPaCalculated { get; private set; }
    public decimal? WeightedUValue { get; private set; }
    public decimal? WeightedSoundDb { get; private set; }
    public DateTime? ValidUntilDate { get; private set; }
    public int CurrentSceneVersion { get; private set; }
    public string? Notes { get; private set; }

    public bool IsBomStale { get; private set; }
    public string? BomStaleReason { get; private set; }
    public DateTime? StaleSinceUtc { get; private set; }

    public EnclosureCategory EnclosureCategory { get; private set; } = EnclosureCategory.Vertical;
    public EnclosureSubtype EnclosureSubtype { get; private set; } = EnclosureSubtype.Balcony;
    public GeometryMode GeometryMode { get; private set; } = GeometryMode.Planar;
    public MountingTopology MountingTopology { get; private set; } = MountingTopology.ProfileFramed;
    public decimal? RoofPitchDeg { get; private set; }
    public int? RidgeHeightMm { get; private set; }
    public int? EaveHeightMm { get; private set; }
    public string? CurtainWallCassetteSpecJson { get; private set; }
    public string? PolygonVerticesJson { get; private set; }
    public string? MetadataJson { get; private set; }

    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    private readonly List<GlassProjectRun> _runs = new();
    public IReadOnlyCollection<GlassProjectRun> Runs => _runs;

    private readonly List<RunConnection> _connections = new();
    public IReadOnlyCollection<RunConnection> Connections => _connections;

    protected GlassProject() { }

    public GlassProject(
        string code,
        Guid customerId,
        string projectName,
        Guid createdByUserId,
        string currency = "TRY")
    {
        Code = code;
        CustomerId = customerId;
        ProjectName = projectName;
        CreatedByUserId = createdByUserId;
        Currency = currency;
        AddDomainEvent(new GlassProjectCreatedEvent(TenantId, Id, customerId, createdByUserId, DateTime.UtcNow));
    }

    public void UpdateHeader(
        string projectName,
        string? siteAddressLine1,
        string? siteAddressLine2,
        string? siteCity,
        string? siteDistrict,
        string? sitePostalCode,
        string? siteCountryCode,
        int? floorNumber,
        decimal? buildingHeightM,
        Guid? windZoneId,
        Guid? climateZoneId,
        string? fireSafetyClass,
        bool scaffoldingRequired,
        bool craneRequired,
        DateTime? validUntilDate,
        string? notes)
    {
        ProjectName = projectName;
        SiteAddressLine1 = siteAddressLine1;
        SiteAddressLine2 = siteAddressLine2;
        SiteCity = siteCity;
        SiteDistrict = siteDistrict;
        SitePostalCode = sitePostalCode;
        SiteCountryCode = siteCountryCode;
        FloorNumber = floorNumber;
        BuildingHeightM = buildingHeightM;
        WindZoneId = windZoneId;
        ClimateZoneId = climateZoneId;
        FireSafetyClass = fireSafetyClass;
        ScaffoldingRequired = scaffoldingRequired;
        CraneRequired = craneRequired;
        ValidUntilDate = validUntilDate;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignTeam(Guid? designerUserId, Guid? salespersonUserId)
    {
        AssignedDesignerUserId = designerUserId;
        AssignedSalespersonUserId = salespersonUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ConfigureEnclosure(
        EnclosureCategory category,
        EnclosureSubtype subtype,
        GeometryMode geometryMode,
        MountingTopology mountingTopology,
        decimal? roofPitchDeg = null,
        int? ridgeHeightMm = null,
        int? eaveHeightMm = null,
        string? curtainWallCassetteSpecJson = null,
        string? polygonVerticesJson = null,
        string? metadataJson = null)
    {
        EnclosureCategory = category;
        EnclosureSubtype = subtype;
        GeometryMode = geometryMode;
        MountingTopology = mountingTopology;
        RoofPitchDeg = roofPitchDeg;
        RidgeHeightMm = ridgeHeightMm;
        EaveHeightMm = eaveHeightMm;
        CurtainWallCassetteSpecJson = curtainWallCassetteSpecJson;
        PolygonVerticesJson = polygonVerticesJson;
        MetadataJson = metadataJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddRun(GlassProjectRun run) => _runs.Add(run);
    public void RemoveRun(Guid runId)
    {
        var run = _runs.FirstOrDefault(r => r.Id == runId);
        if (run is not null) _runs.Remove(run);
    }

    public void AddConnection(RunConnection connection) => _connections.Add(connection);
    public void RemoveConnection(Guid connectionId)
    {
        var conn = _connections.FirstOrDefault(c => c.Id == connectionId);
        if (conn is not null) _connections.Remove(conn);
    }

    public void TransitionTo(GlassProjectStatus next, Guid changedByUserId)
    {
        EnsureAllowedTransition(Status, next);
        var previous = Status;
        Status = next;
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(new GlassProjectStatusChangedEvent(TenantId, Id, previous, next, changedByUserId, DateTime.UtcNow));
    }

    public void RecordCalculations(decimal totalAreaM2, int totalPanels, decimal windLoadPa, decimal weightedU, decimal weightedDb)
    {
        TotalAreaM2 = totalAreaM2;
        TotalPanels = totalPanels;
        WindLoadPaCalculated = windLoadPa;
        WeightedUValue = weightedU;
        WeightedSoundDb = weightedDb;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordTotals(decimal subtotal, decimal discountTotal, decimal taxTotal, decimal grandTotal)
    {
        Subtotal = subtotal;
        DiscountTotal = discountTotal;
        TaxTotal = taxTotal;
        GrandTotal = grandTotal;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void LockFxRate(decimal fxRateToBase)
    {
        FxRateToBase = fxRateToBase;
        FxRateLockedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AdvanceSceneVersion(int newVersion)
    {
        if (newVersion <= CurrentSceneVersion)
        {
            throw new InvalidOperationException("Scene version must be monotonically increasing.");
        }
        CurrentSceneVersion = newVersion;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RaiseQuotedDomainEvent(Guid quoteSnapshotId, string shareToken)
    {
        AddDomainEvent(new GlassProjectQuotedEvent(TenantId, Id, quoteSnapshotId, shareToken, DateTime.UtcNow));
    }

    public void MarkBomStale(string reason, DateTime utcNow)
    {
        IsBomStale = true;
        BomStaleReason = reason.Length > 32 ? reason.Substring(0, 32) : reason;
        if (!StaleSinceUtc.HasValue) StaleSinceUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkBomFresh(DateTime utcNow)
    {
        IsBomStale = false;
        BomStaleReason = null;
        StaleSinceUtc = null;
        UpdatedAtUtc = utcNow;
    }

    private static void EnsureAllowedTransition(GlassProjectStatus from, GlassProjectStatus to)
    {
        var allowed = from switch
        {
            GlassProjectStatus.Draft => to is GlassProjectStatus.Surveyed or GlassProjectStatus.Quoted or GlassProjectStatus.Cancelled,
            GlassProjectStatus.Surveyed => to is GlassProjectStatus.Draft or GlassProjectStatus.Quoted or GlassProjectStatus.Cancelled,
            GlassProjectStatus.Quoted => to is GlassProjectStatus.Draft or GlassProjectStatus.Confirmed or GlassProjectStatus.Cancelled,
            GlassProjectStatus.Confirmed => to is GlassProjectStatus.InProduction or GlassProjectStatus.Cancelled,
            GlassProjectStatus.InProduction => to is GlassProjectStatus.Ready or GlassProjectStatus.Defective,
            GlassProjectStatus.Ready => to is GlassProjectStatus.Installed or GlassProjectStatus.InTransit,
            GlassProjectStatus.InTransit => to is GlassProjectStatus.Installed or GlassProjectStatus.Defective,
            GlassProjectStatus.Defective => to is GlassProjectStatus.InProduction or GlassProjectStatus.Cancelled,
            _ => false,
        };
        if (!allowed)
        {
            throw new GlassProjectInvalidStatusTransitionException(from.ToString(), to.ToString());
        }
    }
}
