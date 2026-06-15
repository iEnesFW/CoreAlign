namespace CoreAlign.Domain.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    Guid? DeletedByUserId { get; set; }
    string? DeletedReason { get; set; }
    void MarkDeleted(Guid? userId, string? reason, DateTime utcNow);
    void Restore();
}
