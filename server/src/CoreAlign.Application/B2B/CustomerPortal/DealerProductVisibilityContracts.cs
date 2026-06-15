using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.B2B.CustomerPortal;

public record GetDealerProductVisibilityQuery(Guid DealerCustomerLinkId)
    : IRequest<DealerProductVisibilityDto>;

public record SetDealerProductVisibilityCommand(
    Guid DealerCustomerLinkId,
    string Mode,
    IReadOnlyList<Guid> ProductIds)
    : IRequest<DealerProductVisibilityDto>, ITransactionalRequest;

public class DealerProductVisibilityDto
{
    public Guid LinkId { get; set; }
    public string Mode { get; set; } = "All";
    public List<Guid> VisibleProductIds { get; set; } = new();
}

public static class DealerProductVisibilityModes
{
    public const string All = "All";
    public const string Whitelist = "Whitelist";
}
