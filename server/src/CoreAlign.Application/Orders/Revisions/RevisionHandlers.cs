using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.Revisions;

internal enum RevisionCallerRole
{
    Tenant,
    Customer,
    Dealer
}

internal sealed record RevisionCallerScope(Guid UserId, RevisionCallerRole Role);

internal static class RevisionCallerResolver
{
    public static async Task<RevisionCallerScope> ResolveAsync(
        Order order,
        ICurrentUserAccessor currentUser,
        IPortalScopeService portalScope,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserIdOrThrow();

        var customerId = await portalScope.TryGetCurrentCustomerIdAsync(cancellationToken);
        if (customerId is not null)
        {
            if (customerId.Value != order.CustomerId)
            {
                throw new OrderNotFoundException();
            }
            return new RevisionCallerScope(userId, RevisionCallerRole.Customer);
        }

        var dealerAccountId = await portalScope.TryGetCurrentDealerAccountIdAsync(cancellationToken);
        if (dealerAccountId is not null)
        {
            if (order.OriginDealerAccountId != dealerAccountId.Value)
            {
                throw new OrderNotFoundException();
            }
            return new RevisionCallerScope(userId, RevisionCallerRole.Dealer);
        }

        return new RevisionCallerScope(userId, RevisionCallerRole.Tenant);
    }

    public static string ToPersona(RevisionCallerRole role) => role switch
    {
        RevisionCallerRole.Customer => OrderOriginPersona.Customer,
        RevisionCallerRole.Dealer => OrderOriginPersona.Dealer,
        _ => OrderOriginPersona.Tenant,
    };

    public static void EnsureCounterparty(string requesterPersona, RevisionCallerRole callerRole)
    {
        if (string.Equals(requesterPersona, OrderOriginPersona.Tenant, StringComparison.Ordinal))
        {
            return;
        }

        var requesterIsDealer = string.Equals(requesterPersona, OrderOriginPersona.Dealer, StringComparison.Ordinal);
        var allowed = requesterIsDealer
            ? callerRole is RevisionCallerRole.Customer or RevisionCallerRole.Tenant
            : callerRole is RevisionCallerRole.Dealer or RevisionCallerRole.Tenant;

        if (!allowed)
        {
            throw new RevisionPersonaNotAuthorizedException(requesterPersona, "approve revision");
        }
    }
}

public class RequestOrderRevisionHandler : IRequestHandler<RequestOrderRevisionCommand, OrderRevisionDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPortalScopeService _portalScope;
    private readonly ITenantContext _tenant;
    private readonly IOrderRevisionOutbox _outbox;

    public RequestOrderRevisionHandler(
        IOrderRepository orders,
        IUnitOfWork uow,
        ICurrentUserAccessor currentUser,
        IPortalScopeService portalScope,
        ITenantContext tenant,
        IOrderRevisionOutbox outbox)
    {
        _orders = orders;
        _uow = uow;
        _currentUser = currentUser;
        _portalScope = portalScope;
        _tenant = tenant;
        _outbox = outbox;
    }

    public async Task<OrderRevisionDto> Handle(RequestOrderRevisionCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetWithLinesAndRevisionsAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        var caller = await RevisionCallerResolver.ResolveAsync(order, _currentUser, _portalScope, cancellationToken);
        var persona = RevisionCallerResolver.ToPersona(caller.Role);

        var snapshots = request.ProposedLines
            .OrderBy(l => l.LineNumber == 0 ? int.MaxValue : l.LineNumber)
            .Select((l, idx) => BuildSnapshot(order, l, idx + 1))
            .ToList();

        var revision = order.RequestRevision(caller.UserId, persona, snapshots, request.RequestNotes, DateTime.UtcNow);
        _orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        await _outbox.EnqueueRequestedAsync(
            new OrderRevisionRequestedPayload(
                TenantId: _tenant.RequireTenantId(),
                OrderId: order.Id,
                RevisionId: revision.Id,
                RevisionNumber: revision.RevisionNumber,
                OrderNumber: order.OrderNumber,
                RequestedByUserId: caller.UserId,
                RequestedByPersona: persona,
                CustomerId: order.CustomerId,
                OriginDealerAccountId: order.OriginDealerAccountId,
                OriginDealerUserId: order.OriginDealerUserId,
                OriginCustomerUserId: order.OriginCustomerUserId),
            cancellationToken);

        return RevisionMapper.ToDto(revision);
    }

    private static RevisionLineSnapshot BuildSnapshot(Order order, RevisionLineInput input, int lineNumber)
    {
        var existing = order.Lines.FirstOrDefault(l => l.ProductId == input.ProductId);
        return new RevisionLineSnapshot
        {
            ProductId = input.ProductId,
            ProductSku = existing?.ProductSku ?? string.Empty,
            ProductName = existing?.ProductName ?? string.Empty,
            LineNumber = input.LineNumber > 0 ? input.LineNumber : lineNumber,
            Quantity = input.Quantity,
            UnitPrice = input.UnitPrice,
            LineDiscountPercent = input.LineDiscountPercent,
            LineDiscountAmount = input.LineDiscountAmount,
            TaxRatePercent = input.TaxRatePercent,
            IsTaxInclusive = input.IsTaxInclusive,
            WithholdingRatePercent = input.WithholdingRatePercent,
            LineNotes = input.LineNotes,
        };
    }
}

public class ApproveOrderRevisionHandler : IRequestHandler<ApproveOrderRevisionCommand, OrderRevisionDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPortalScopeService _portalScope;
    private readonly ITenantContext _tenant;
    private readonly IOrderRevisionOutbox _outbox;

    public ApproveOrderRevisionHandler(
        IOrderRepository orders,
        IUnitOfWork uow,
        ICurrentUserAccessor currentUser,
        IPortalScopeService portalScope,
        ITenantContext tenant,
        IOrderRevisionOutbox outbox)
    {
        _orders = orders;
        _uow = uow;
        _currentUser = currentUser;
        _portalScope = portalScope;
        _tenant = tenant;
        _outbox = outbox;
    }

    public async Task<OrderRevisionDto> Handle(ApproveOrderRevisionCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetWithLinesAndRevisionsAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        var caller = await RevisionCallerResolver.ResolveAsync(order, _currentUser, _portalScope, cancellationToken);

        var revision = order.Revisions.FirstOrDefault(r => r.Id == request.RevisionId)
            ?? throw new OrderRevisionNotFoundException();

        RevisionCallerResolver.EnsureCounterparty(revision.RequestedByPersona, caller.Role);

        order.ApplyRevision(revision.Id, caller.UserId, DateTime.UtcNow);
        _orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        await _outbox.EnqueueApprovedAsync(
            new OrderRevisionApprovedPayload(
                TenantId: _tenant.RequireTenantId(),
                OrderId: order.Id,
                RevisionId: revision.Id,
                RevisionNumber: revision.RevisionNumber,
                OrderNumber: order.OrderNumber,
                ApprovedByUserId: caller.UserId,
                RequestedByUserId: revision.RequestedByUserId,
                RequestedByPersona: revision.RequestedByPersona,
                CustomerId: order.CustomerId,
                NewTotal: order.Total,
                Currency: order.Currency),
            cancellationToken);

        return RevisionMapper.ToDto(revision);
    }
}

public class RejectOrderRevisionHandler : IRequestHandler<RejectOrderRevisionCommand, OrderRevisionDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPortalScopeService _portalScope;
    private readonly ITenantContext _tenant;
    private readonly IOrderRevisionOutbox _outbox;

    public RejectOrderRevisionHandler(
        IOrderRepository orders,
        IUnitOfWork uow,
        ICurrentUserAccessor currentUser,
        IPortalScopeService portalScope,
        ITenantContext tenant,
        IOrderRevisionOutbox outbox)
    {
        _orders = orders;
        _uow = uow;
        _currentUser = currentUser;
        _portalScope = portalScope;
        _tenant = tenant;
        _outbox = outbox;
    }

    public async Task<OrderRevisionDto> Handle(RejectOrderRevisionCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetWithLinesAndRevisionsAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        var caller = await RevisionCallerResolver.ResolveAsync(order, _currentUser, _portalScope, cancellationToken);

        var revision = order.Revisions.FirstOrDefault(r => r.Id == request.RevisionId)
            ?? throw new OrderRevisionNotFoundException();

        RevisionCallerResolver.EnsureCounterparty(revision.RequestedByPersona, caller.Role);

        order.RejectRevision(revision.Id, caller.UserId, request.Reason, DateTime.UtcNow);
        _orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        await _outbox.EnqueueRejectedAsync(
            new OrderRevisionRejectedPayload(
                TenantId: _tenant.RequireTenantId(),
                OrderId: order.Id,
                RevisionId: revision.Id,
                RevisionNumber: revision.RevisionNumber,
                OrderNumber: order.OrderNumber,
                RejectedByUserId: caller.UserId,
                Reason: request.Reason,
                RequestedByUserId: revision.RequestedByUserId),
            cancellationToken);

        return RevisionMapper.ToDto(revision);
    }
}

public class CancelOrderRevisionHandler : IRequestHandler<CancelOrderRevisionCommand, OrderRevisionDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPortalScopeService _portalScope;

    public CancelOrderRevisionHandler(
        IOrderRepository orders,
        IUnitOfWork uow,
        ICurrentUserAccessor currentUser,
        IPortalScopeService portalScope)
    {
        _orders = orders;
        _uow = uow;
        _currentUser = currentUser;
        _portalScope = portalScope;
    }

    public async Task<OrderRevisionDto> Handle(CancelOrderRevisionCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetWithLinesAndRevisionsAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        var caller = await RevisionCallerResolver.ResolveAsync(order, _currentUser, _portalScope, cancellationToken);

        var revision = order.Revisions.FirstOrDefault(r => r.Id == request.RevisionId)
            ?? throw new OrderRevisionNotFoundException();

        order.CancelRevision(revision.Id, caller.UserId, DateTime.UtcNow);
        _orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        return RevisionMapper.ToDto(revision);
    }
}

public class GetOrderRevisionsHandler : IRequestHandler<GetOrderRevisionsQuery, OrderRevisionTimelineDto>
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IPortalScopeService _portalScope;

    public GetOrderRevisionsHandler(
        IOrderRepository orders,
        ICurrentUserAccessor currentUser,
        IPortalScopeService portalScope)
    {
        _orders = orders;
        _currentUser = currentUser;
        _portalScope = portalScope;
    }

    public async Task<OrderRevisionTimelineDto> Handle(GetOrderRevisionsQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetWithLinesAndRevisionsAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();
        await RevisionCallerResolver.ResolveAsync(order, _currentUser, _portalScope, cancellationToken);
        return RevisionMapper.ToTimelineDto(order);
    }
}
