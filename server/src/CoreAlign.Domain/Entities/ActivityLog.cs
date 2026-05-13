using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class ActivityLog : TenantEntity
{
    public Guid? UserId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int DurationMs { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? TraceId { get; set; }
}
