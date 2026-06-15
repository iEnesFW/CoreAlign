namespace CoreAlign.Domain.Common;

public static class SoftDeletableExtensions
{
    public static void MarkDeletedInternal(this ISoftDeletable entity, Guid? userId, string? reason, DateTime utcNow)
    {
        entity.IsDeleted = true;
        entity.DeletedAtUtc = utcNow;
        entity.DeletedByUserId = userId;
        entity.DeletedReason = reason;
    }

    public static void RestoreInternal(this ISoftDeletable entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAtUtc = null;
        entity.DeletedByUserId = null;
        entity.DeletedReason = null;
    }
}
