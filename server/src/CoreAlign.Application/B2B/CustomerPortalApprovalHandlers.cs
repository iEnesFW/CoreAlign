using CoreAlign.Application.B2B.DealerOrderFlow;
using CoreAlign.Application.Common;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B;

public class GetCustomerPortalPendingApprovalsHandler
    : IRequestHandler<GetCustomerPortalPendingApprovalsQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;

    public GetCustomerPortalPendingApprovalsHandler(IPortalScopeService scope, IOrderRepository orders)
    {
        _scope = scope;
        _orders = orders;
    }

    public async Task<PagedResult<OrderSummaryDto>> Handle(GetCustomerPortalPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _orders.SearchPendingApprovalsForCustomerAsync(customerId, page, pageSize, cancellationToken);

        return new PagedResult<OrderSummaryDto>
        {
            Items = items.Select(OrderMapper.ToSummaryDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetCustomerPortalApprovalByIdHandler : IRequestHandler<GetCustomerPortalApprovalByIdQuery, OrderDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;
    private readonly IDealerAccountRepository _dealers;
    private readonly IUserRepository _users;

    public GetCustomerPortalApprovalByIdHandler(
        IPortalScopeService scope,
        IOrderRepository orders,
        IDealerAccountRepository dealers,
        IUserRepository users)
    {
        _scope = scope;
        _orders = orders;
        _dealers = dealers;
        _users = users;
    }

    public async Task<OrderDto> Handle(GetCustomerPortalApprovalByIdQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var order = await _orders.GetWithLinesAsync(request.OrderId, cancellationToken);

        // Treat any cross-customer access exactly like 404 — never leak whether
        // an order id exists for another customer in the same tenant.
        if (order is null || order.CustomerId != customerId)
        {
            throw new OrderNotFoundException();
        }

        string? dealerName = null;
        if (order.OriginDealerAccountId is Guid did)
        {
            dealerName = (await _dealers.GetByIdAsync(did, cancellationToken))?.Name;
        }

        string? approvedByName = null;
        if (order.DealerApprovedByUserId is Guid uid)
        {
            var u = await _users.GetByIdAsync(uid, cancellationToken);
            if (u is not null)
            {
                var first = u.FirstName?.Trim();
                var last = u.LastName?.Trim();
                approvedByName = !string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last)
                    ? string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrEmpty(s)))
                    : (!string.IsNullOrWhiteSpace(u.Username) ? u.Username : u.Email);
            }
        }

        return OrderMapper.ToDto(order, dealerName, approvedByName);
    }
}

public class ApproveDealerOrderHandler : IRequestHandler<ApproveDealerOrderCommand, OrderDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantContext _tenant;
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IDealerAccountRepository _dealers;
    private readonly IUnitOfWork _uow;
    private readonly IDealerOrderApprovalOutbox _outbox;

    public ApproveDealerOrderHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        ITenantContext tenant,
        IOrderRepository orders,
        ICustomerRepository customers,
        IDealerAccountRepository dealers,
        IUnitOfWork uow,
        IDealerOrderApprovalOutbox outbox)
    {
        _scope = scope;
        _currentUser = currentUser;
        _tenant = tenant;
        _orders = orders;
        _customers = customers;
        _dealers = dealers;
        _uow = uow;
        _outbox = outbox;
    }

    public async Task<OrderDto> Handle(ApproveDealerOrderCommand request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var callerUserId = _currentUser.UserIdOrThrow();
        var tenantId = _tenant.RequireTenantId();

        var order = await _orders.GetWithLinesAsync(request.OrderId, cancellationToken);
        if (order is null || order.CustomerId != customerId)
        {
            throw new OrderNotFoundException();
        }

        if (!order.IsPendingDealerApproval)
        {
            throw new InvalidOrderApprovalStateException(
                $"Order {order.Id} cannot be approved from approval state '{order.DealerApprovalStatus ?? "<none>"}'.");
        }

        order.ApproveDealerSubmission(callerUserId);

        // Reuse the existing Submit transition so all downstream side-effects
        // (events, audit, eventual confirm/allocate) work identically to a
        // tenant-staff-submitted order.
        order.Submit();

        await _uow.SaveChangesAsync(cancellationToken);

        var customer = await _customers.GetByIdAsync(customerId, cancellationToken);
        var dealer = order.OriginDealerAccountId is Guid did
            ? await _dealers.GetByIdAsync(did, cancellationToken)
            : null;

        await _outbox.EnqueueApprovedAsync(
            new DealerOrderApprovedByCustomerPayload(
                OrderId: order.Id,
                TenantId: tenantId,
                CustomerId: customerId,
                CustomerName: customer?.Name ?? string.Empty,
                DealerAccountId: order.OriginDealerAccountId ?? Guid.Empty,
                DealerName: dealer?.Name ?? string.Empty,
                DealerUserId: order.OriginDealerUserId,
                ApprovedByUserId: callerUserId,
                LineCount: order.Lines.Count,
                Total: order.Total,
                Currency: order.Currency),
            cancellationToken);

        return OrderMapper.ToDto(order, dealer?.Name, null);
    }
}

public class RejectDealerOrderHandler : IRequestHandler<RejectDealerOrderCommand, OrderDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantContext _tenant;
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IDealerAccountRepository _dealers;
    private readonly IUnitOfWork _uow;
    private readonly IDealerOrderApprovalOutbox _outbox;

    public RejectDealerOrderHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        ITenantContext tenant,
        IOrderRepository orders,
        ICustomerRepository customers,
        IDealerAccountRepository dealers,
        IUnitOfWork uow,
        IDealerOrderApprovalOutbox outbox)
    {
        _scope = scope;
        _currentUser = currentUser;
        _tenant = tenant;
        _orders = orders;
        _customers = customers;
        _dealers = dealers;
        _uow = uow;
        _outbox = outbox;
    }

    public async Task<OrderDto> Handle(RejectDealerOrderCommand request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var callerUserId = _currentUser.UserIdOrThrow();
        var tenantId = _tenant.RequireTenantId();

        var order = await _orders.GetWithLinesAsync(request.OrderId, cancellationToken);
        if (order is null || order.CustomerId != customerId)
        {
            throw new OrderNotFoundException();
        }

        if (!order.IsPendingDealerApproval)
        {
            throw new InvalidOrderApprovalStateException(
                $"Order {order.Id} cannot be rejected from approval state '{order.DealerApprovalStatus ?? "<none>"}'.");
        }

        order.RejectDealerSubmission(callerUserId, request.Reason);
        order.Cancel(request.Reason);

        await _uow.SaveChangesAsync(cancellationToken);

        var customer = await _customers.GetByIdAsync(customerId, cancellationToken);
        var dealer = order.OriginDealerAccountId is Guid did
            ? await _dealers.GetByIdAsync(did, cancellationToken)
            : null;

        await _outbox.EnqueueRejectedAsync(
            new DealerOrderRejectedByCustomerPayload(
                OrderId: order.Id,
                TenantId: tenantId,
                CustomerId: customerId,
                CustomerName: customer?.Name ?? string.Empty,
                DealerAccountId: order.OriginDealerAccountId ?? Guid.Empty,
                DealerName: dealer?.Name ?? string.Empty,
                DealerUserId: order.OriginDealerUserId,
                RejectedByUserId: callerUserId,
                Reason: request.Reason),
            cancellationToken);

        return OrderMapper.ToDto(order, dealer?.Name, null);
    }
}
