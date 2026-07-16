using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class UserWarehouseAccess : TenantEntity
{
    public Guid UserId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid GrantedByUserId { get; private set; }

    public Warehouse Warehouse { get; set; } = null!;

    protected UserWarehouseAccess() { }

    public UserWarehouseAccess(Guid userId, Guid warehouseId, Guid grantedByUserId)
    {
        UserId = userId;
        WarehouseId = warehouseId;
        GrantedByUserId = grantedByUserId;
    }
}
