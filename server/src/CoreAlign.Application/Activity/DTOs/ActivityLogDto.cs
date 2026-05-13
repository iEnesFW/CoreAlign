namespace CoreAlign.Application.Activity.DTOs;

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int DurationMs { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? TraceId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
