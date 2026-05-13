namespace CoreAlign.Domain.Common;

public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
