using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Whitelabel;

public class TenantTheme : ITenantOwned, IHasConcurrencyToken
{
    public Guid TenantId { get; set; }
    public Guid? LogoFileId { get; private set; }
    public Guid? FaviconFileId { get; private set; }
    public string PrimaryColor { get; private set; } = "#0EA5E9";
    public string AccentColor { get; private set; } = "#22D3EE";
    public string? BrandName { get; private set; }
    public string? CustomSubdomain { get; private set; }
    public string? CustomDomain { get; private set; }
    public string EmailFromName { get; private set; } = "CoreAlign";
    public string? EmailFromAddress { get; private set; }
    public Guid? LoginBackgroundFileId { get; private set; }
    public string? LoginHeadingMd { get; private set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected TenantTheme() { }

    public TenantTheme(Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        TenantId = tenantId;
    }

    public void UpdateColors(string primaryColor, string accentColor, DateTime nowUtc)
    {
        if (!IsValidHexColor(primaryColor)) throw new ArgumentException("PrimaryColor must be a hex color (#RRGGBB).", nameof(primaryColor));
        if (!IsValidHexColor(accentColor)) throw new ArgumentException("AccentColor must be a hex color (#RRGGBB).", nameof(accentColor));
        PrimaryColor = primaryColor.Trim().ToUpperInvariant();
        AccentColor = accentColor.Trim().ToUpperInvariant();
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateBranding(string? brandName, string emailFromName, string? emailFromAddress, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(emailFromName)) throw new ArgumentException("EmailFromName is required.", nameof(emailFromName));
        BrandName = brandName?.Trim();
        EmailFromName = emailFromName.Trim();
        EmailFromAddress = string.IsNullOrWhiteSpace(emailFromAddress) ? null : emailFromAddress.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateDomains(string? customSubdomain, string? customDomain, DateTime nowUtc)
    {
        CustomSubdomain = NormalizeSubdomain(customSubdomain);
        CustomDomain = string.IsNullOrWhiteSpace(customDomain) ? null : customDomain.Trim().ToLowerInvariant();
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateLoginPage(string? loginHeadingMd, DateTime nowUtc)
    {
        LoginHeadingMd = string.IsNullOrWhiteSpace(loginHeadingMd) ? null : loginHeadingMd.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void SetAssetFileId(Domain.Enums.TenantThemeAssetKind kind, Guid? fileId, DateTime nowUtc)
    {
        switch (kind)
        {
            case Domain.Enums.TenantThemeAssetKind.Logo:
                LogoFileId = fileId;
                break;
            case Domain.Enums.TenantThemeAssetKind.Favicon:
                FaviconFileId = fileId;
                break;
            case Domain.Enums.TenantThemeAssetKind.LoginBackground:
                LoginBackgroundFileId = fileId;
                break;
            case Domain.Enums.TenantThemeAssetKind.EmailHeader:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported asset kind.");
        }
        UpdatedAtUtc = nowUtc;
    }

    private static bool IsValidHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        if (trimmed.Length != 7 || trimmed[0] != '#') return false;
        for (var i = 1; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }
        return true;
    }

    private static string? NormalizeSubdomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim().ToLowerInvariant();
        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            var ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-';
            if (!ok) throw new ArgumentException("CustomSubdomain may contain only lowercase letters, digits and hyphens.", nameof(value));
        }
        return trimmed;
    }
}
