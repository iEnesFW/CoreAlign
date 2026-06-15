using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class CustomerTagLink : TenantEntity
{
    public Guid CustomerId { get; set; }
    public Guid TagId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Tag Tag { get; set; } = null!;

    protected CustomerTagLink() { }

    public CustomerTagLink(Guid customerId, Guid tagId)
    {
        CustomerId = customerId;
        TagId = tagId;
    }
}
