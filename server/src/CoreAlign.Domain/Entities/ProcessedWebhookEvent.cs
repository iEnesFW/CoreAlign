namespace CoreAlign.Domain.Entities;

public class ProcessedWebhookEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Gateway { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
