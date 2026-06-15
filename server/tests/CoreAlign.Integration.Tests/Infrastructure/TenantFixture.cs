namespace CoreAlign.Integration.Tests.Infrastructure;

public sealed class TenantFixture
{
    public required Guid TenantId { get; init; }
    public required string TenantSlug { get; init; }

    public required Guid TenantAdminUserId { get; init; }
    public required string TenantAdminEmail { get; init; }

    public required Guid CustomerId { get; init; }
    public required Guid CustomerUserId { get; init; }
    public required string CustomerUserEmail { get; init; }

    public required Guid DealerAccountId { get; init; }
    public required Guid DealerUserId { get; init; }
    public required string DealerUserEmail { get; init; }

    public required Guid Product1Id { get; init; }
    public required Guid Product2Id { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid InvoiceId { get; init; }
    public required Guid PaymentId { get; init; }
    public required Guid NotificationCustomerId { get; init; }
    public required Guid NotificationDealerId { get; init; }
}
