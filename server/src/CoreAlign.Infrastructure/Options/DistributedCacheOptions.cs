namespace CoreAlign.Infrastructure.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public bool Enabled { get; set; }

    public string? ConnectionString { get; set; }

    public string InstanceName { get; set; } = "corealign-";
}

public class CacheRegionOptions
{
    public const string SectionName = "Cache:Regions";

    public int DashboardTtlSeconds { get; set; } = 30;

    public int LookupsTtlSeconds { get; set; } = 300;

    public int CustomReportDataTtlSeconds { get; set; } = 60;

    public int GenericTtlSeconds { get; set; } = 60;
}
