using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class CustomerDealerProductVisibility : TenantEntity
{
    public Guid DealerCustomerLinkId { get; private set; }
    public Guid ProductId { get; private set; }

    public DealerCustomerLink DealerCustomerLink { get; set; } = null!;
    public Product Product { get; set; } = null!;

    protected CustomerDealerProductVisibility() { }

    public CustomerDealerProductVisibility(Guid dealerCustomerLinkId, Guid productId)
    {
        DealerCustomerLinkId = dealerCustomerLinkId;
        ProductId = productId;
    }
}
