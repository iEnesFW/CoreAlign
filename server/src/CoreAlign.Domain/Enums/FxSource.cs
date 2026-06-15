namespace CoreAlign.Domain.Enums;

public enum FxSource
{
    Tcmb = 0,
    Ecb = 1,
    Manual = 2,
    TenantOverride = 3,
}

public static class FxSourceCodes
{
    public const string Tcmb = "TCMB";
    public const string Ecb = "ECB";
    public const string Manual = "MANUAL";
    public const string TenantOverride = "TENANT_OVERRIDE";

    public static string ToCode(FxSource source) => source switch
    {
        FxSource.Tcmb => Tcmb,
        FxSource.Ecb => Ecb,
        FxSource.Manual => Manual,
        FxSource.TenantOverride => TenantOverride,
        _ => Tcmb,
    };

    public static FxSource Parse(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return FxSource.Tcmb;
        return code.Trim().ToUpperInvariant() switch
        {
            Tcmb => FxSource.Tcmb,
            Ecb => FxSource.Ecb,
            Manual => FxSource.Manual,
            TenantOverride => FxSource.TenantOverride,
            _ => FxSource.Tcmb,
        };
    }
}
