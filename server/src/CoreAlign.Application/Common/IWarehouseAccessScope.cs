namespace CoreAlign.Application.Common;

// WHY: Unrestricted is an explicit signal (admin / flag off), NOT an empty list — an empty allowed
// set means the restricted user may see nothing (deny-by-default).
public sealed record WarehouseAccessResult(bool IsUnrestricted, IReadOnlyList<Guid> AllowedWarehouseIds)
{
    public static readonly WarehouseAccessResult Unrestricted = new(true, Array.Empty<Guid>());

    public static WarehouseAccessResult Restricted(IReadOnlyList<Guid> allowedWarehouseIds) =>
        new(false, allowedWarehouseIds);
}

public interface IWarehouseAccessScope
{
    Task<WarehouseAccessResult> GetAllowedWarehouseIdsAsync(CancellationToken cancellationToken = default);
}
