using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassEnclosureSettings : TenantEntity
{
    public int DefaultStockBarLengthMm { get; private set; } = 6000;
    public int DefaultJumboGlassWidthMm { get; private set; } = 3210;
    public int DefaultJumboGlassHeightMm { get; private set; } = 2250;
    public decimal SawKerfMm { get; private set; } = 5m;
    public decimal GlassKerfMm { get; private set; } = 4m;
    public bool GuillotineRequired { get; private set; } = true;
    public decimal DefaultWastePercent { get; private set; } = 5m;
    public decimal LaborCostPerM2 { get; private set; }
    public decimal DefaultMarginPercent { get; private set; } = 20m;
    public decimal BendRailFeePerM { get; private set; } = 150m;
    public decimal BentGlassCostFactor { get; private set; } = 2.75m;
    public int FieldToleranceTopMm { get; private set; } = 10;
    public int FieldToleranceSideMm { get; private set; } = 5;
    public decimal TransportRatePerKm { get; private set; }
    public decimal TransportRatePerKg { get; private set; }
    public int ScaffoldingRequiredFromFloor { get; private set; } = 5;
    public decimal ScaffoldingRatePerM2 { get; private set; }
    public int CraneRequiredFromFloor { get; private set; } = 10;
    public decimal CraneRatePerMeter { get; private set; }
    public decimal WorkshopDailyCapacityM2 { get; private set; } = 80m;
    public string DefaultPaymentTermsJson { get; private set; } = "[]";
    public string DefaultLocale { get; private set; } = "tr-TR";
    public string DefaultCurrency { get; private set; } = "TRY";
    public int DataRetentionDays { get; private set; } = 730;
    public string? WhatsappBusinessPhoneId { get; private set; }
    public string? NotificationEmailFrom { get; private set; }
    public int QuoteShareTokenTtlDays { get; private set; } = 30;
    public bool OnboardingComplete { get; private set; }
    public string OnboardingStateJson { get; private set; } = "{}";

    protected GlassEnclosureSettings() { }

    public GlassEnclosureSettings(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public void UpdateCore(
        int defaultStockBarLengthMm,
        int defaultJumboGlassWidthMm,
        int defaultJumboGlassHeightMm,
        decimal sawKerfMm,
        decimal glassKerfMm,
        bool guillotineRequired,
        decimal defaultWastePercent,
        decimal laborCostPerM2,
        decimal defaultMarginPercent,
        decimal bendRailFeePerM = 150m,
        decimal bentGlassCostFactor = 2.75m)
    {
        DefaultStockBarLengthMm = defaultStockBarLengthMm;
        DefaultJumboGlassWidthMm = defaultJumboGlassWidthMm;
        DefaultJumboGlassHeightMm = defaultJumboGlassHeightMm;
        SawKerfMm = sawKerfMm;
        GlassKerfMm = glassKerfMm;
        GuillotineRequired = guillotineRequired;
        DefaultWastePercent = defaultWastePercent;
        LaborCostPerM2 = laborCostPerM2;
        DefaultMarginPercent = defaultMarginPercent;
        BendRailFeePerM = bendRailFeePerM;
        BentGlassCostFactor = bentGlassCostFactor;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateField(
        int fieldToleranceTopMm,
        int fieldToleranceSideMm)
    {
        FieldToleranceTopMm = fieldToleranceTopMm;
        FieldToleranceSideMm = fieldToleranceSideMm;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateInstallation(
        decimal transportRatePerKm,
        decimal transportRatePerKg,
        int scaffoldingRequiredFromFloor,
        decimal scaffoldingRatePerM2,
        int craneRequiredFromFloor,
        decimal craneRatePerMeter,
        decimal workshopDailyCapacityM2)
    {
        TransportRatePerKm = transportRatePerKm;
        TransportRatePerKg = transportRatePerKg;
        ScaffoldingRequiredFromFloor = scaffoldingRequiredFromFloor;
        ScaffoldingRatePerM2 = scaffoldingRatePerM2;
        CraneRequiredFromFloor = craneRequiredFromFloor;
        CraneRatePerMeter = craneRatePerMeter;
        WorkshopDailyCapacityM2 = workshopDailyCapacityM2;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateLocaleAndCommunication(
        string defaultLocale,
        string defaultCurrency,
        string defaultPaymentTermsJson,
        string? whatsappBusinessPhoneId,
        string? notificationEmailFrom,
        int quoteShareTokenTtlDays,
        int dataRetentionDays)
    {
        DefaultLocale = defaultLocale;
        DefaultCurrency = defaultCurrency;
        DefaultPaymentTermsJson = defaultPaymentTermsJson;
        WhatsappBusinessPhoneId = whatsappBusinessPhoneId;
        NotificationEmailFrom = notificationEmailFrom;
        QuoteShareTokenTtlDays = quoteShareTokenTtlDays;
        DataRetentionDays = dataRetentionDays;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkOnboardingComplete(string onboardingStateJson)
    {
        OnboardingComplete = true;
        OnboardingStateJson = onboardingStateJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateOnboardingState(string onboardingStateJson)
    {
        OnboardingStateJson = onboardingStateJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
