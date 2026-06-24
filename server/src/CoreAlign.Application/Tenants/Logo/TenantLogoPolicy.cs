namespace CoreAlign.Application.Tenants.Logo;

public static class TenantLogoPolicy
{
    public const long MaxBytes = 1024L * 1024L;
    public const string StorageScope = "tenant-logos";
}
