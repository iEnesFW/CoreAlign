using CoreAlign.Application.B2B.DealerOrderFlow;
using CoreAlign.Application.Common;
using CoreAlign.Application.CustomerPortal.Credit;
using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Application.Products.DTOs;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public class GetDealerPortalDashboardHandler : IRequestHandler<GetDealerPortalDashboardQuery, DealerPortalDashboardDto>
{
    private const int RecentLimit = 5;

    private static readonly OrderStatus[] OpenOrderStatuses =
    {
        OrderStatus.Draft,
        OrderStatus.Submitted,
        OrderStatus.Approved,
        OrderStatus.Confirmed,
        OrderStatus.Allocated,
        OrderStatus.Picking,
        OrderStatus.Packed,
        OrderStatus.PartiallyShipped,
        OrderStatus.Shipped,
    };

    private static readonly OrderStatus[] CompletedOrderStatuses =
    {
        OrderStatus.Confirmed,
        OrderStatus.Allocated,
        OrderStatus.Picking,
        OrderStatus.Packed,
        OrderStatus.PartiallyShipped,
        OrderStatus.Shipped,
        OrderStatus.Delivered,
        OrderStatus.Closed,
    };

    private readonly IPortalScopeService _scope;
    private readonly IDealerAccountRepository _dealers;
    private readonly IOrderRepository _orders;

    public GetDealerPortalDashboardHandler(
        IPortalScopeService scope,
        IDealerAccountRepository dealers,
        IOrderRepository orders)
    {
        _scope = scope;
        _dealers = dealers;
        _orders = orders;
    }

    public async Task<DealerPortalDashboardDto> Handle(GetDealerPortalDashboardQuery request, CancellationToken cancellationToken)
    {
        var dealerId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var dealer = await _dealers.GetByIdAsync(dealerId, cancellationToken);
        var allowed = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);

        var (recentRows, _) = await _orders.SearchByDealerAsync(dealerId, null, null, 1, RecentLimit, cancellationToken);

        var pending = recentRows.Count(r =>
            string.Equals(r.DealerApprovalStatus, DealerOrderApprovalStatuses.PendingCustomerApproval, StringComparison.Ordinal));

        var (_, pendingTotal) = await _orders.SearchByDealerAsync(
            dealerId,
            null,
            DealerOrderApprovalStatuses.PendingCustomerApproval,
            1,
            1,
            cancellationToken);
        if (pendingTotal > 0) pending = pendingTotal;

        var openTotal = recentRows.Count(r => OpenOrderStatuses.Contains(r.Status));

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var completedThisMonth = await _orders.CountDealerOrdersByStatusesSinceAsync(
            dealerId, CompletedOrderStatuses, monthStart, cancellationToken);

        var recentOrders = recentRows.Select(OrderMapper.ToSummaryDto).ToList();

        return new DealerPortalDashboardDto(
            DealerAccountId: dealerId,
            DealerAccountName: dealer?.Name ?? string.Empty,
            AllowedCustomerCount: allowed.Count,
            PendingApprovalCount: pending,
            TotalOpenOrders: openTotal,
            OrdersCompletedThisMonth: completedThisMonth,
            RecentOrders: recentOrders);
    }
}

public class ListDealerAllowedCustomersHandler : IRequestHandler<ListDealerAllowedCustomersQuery, IReadOnlyList<DealerAllowedCustomerDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly ICustomerRepository _customers;
    private readonly IPriceListRepository _priceLists;

    public ListDealerAllowedCustomersHandler(
        IPortalScopeService scope,
        ICustomerRepository customers,
        IPriceListRepository priceLists)
    {
        _scope = scope;
        _customers = customers;
        _priceLists = priceLists;
    }

    public async Task<IReadOnlyList<DealerAllowedCustomerDto>> Handle(ListDealerAllowedCustomersQuery request, CancellationToken cancellationToken)
    {
        var allowed = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);
        if (allowed.Count == 0) return Array.Empty<DealerAllowedCustomerDto>();

        // Batch-load customers (one IN query) and resolve price-list names from a
        // single lookup, so DB round-trips stay O(1) regardless of how many
        // customers the dealer is allowed to see — was N+1 (per-id GetByIdAsync).
        var customers = await _customers.GetByIdsAsync(allowed, cancellationToken);
        var priceListNames = (await _priceLists.ListAsync(null, cancellationToken))
            .ToDictionary(p => p.Id, p => p.Name);

        var result = new List<DealerAllowedCustomerDto>(customers.Count);
        foreach (var id in allowed)
        {
            if (!customers.TryGetValue(id, out var customer)) continue;

            var priceListName = customer.PriceListId is Guid plId
                && priceListNames.TryGetValue(plId, out var name)
                ? name
                : null;

            result.Add(new DealerAllowedCustomerDto(
                CustomerId: customer.Id,
                Code: customer.Code,
                Name: customer.Name,
                TaxNumber: customer.TaxNumber,
                Currency: customer.DefaultCurrency,
                DefaultPriceListId: customer.PriceListId,
                DefaultPriceListName: priceListName));
        }

        return result;
    }
}

public class ListDealerOrdersHandler : IRequestHandler<ListDealerOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;

    public ListDealerOrdersHandler(IPortalScopeService scope, IOrderRepository orders)
    {
        _scope = scope;
        _orders = orders;
    }

    public async Task<PagedResult<OrderSummaryDto>> Handle(ListDealerOrdersQuery request, CancellationToken cancellationToken)
    {
        var dealerId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _orders.SearchByDealerAsync(
            dealerId,
            request.Status,
            request.ApprovalStatus,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<OrderSummaryDto>
        {
            Items = items.Select(OrderMapper.ToSummaryDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetDealerOrderByIdHandler : IRequestHandler<GetDealerOrderByIdQuery, OrderDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;
    private readonly IDealerAccountRepository _dealers;
    private readonly IUserRepository _users;

    public GetDealerOrderByIdHandler(
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

    public async Task<OrderDto> Handle(GetDealerOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var dealerId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var order = await _orders.GetWithLinesAsync(request.OrderId, cancellationToken);

        if (order is null || order.OriginDealerAccountId != dealerId)
        {
            throw new OrderNotFoundException();
        }

        var dealerName = order.OriginDealerAccountId.HasValue
            ? (await _dealers.GetByIdAsync(order.OriginDealerAccountId.Value, cancellationToken))?.Name
            : null;
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

public class CreateDealerOrderHandler : IRequestHandler<CreateDealerOrderCommand, OrderDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IDealerUserRepository _dealerUsers;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IPaymentTermRepository _paymentTerms;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IUnitOfWork _uow;
    private readonly IDealerOrderApprovalOutbox _outbox;
    private readonly IDealerAccountRepository _dealers;
    private readonly IPricingService _pricing;
    private readonly ICustomerLedgerRepository _ledger;

    public CreateDealerOrderHandler(
        IPortalScopeService scope,
        IDealerUserRepository dealerUsers,
        ITenantContext tenant,
        ICurrentUserAccessor currentUser,
        IOrderRepository orders,
        ICustomerRepository customers,
        IProductRepository products,
        ICustomerAddressRepository addresses,
        IPaymentTermRepository paymentTerms,
        IDocumentSequenceRepository sequences,
        IUnitOfWork uow,
        IDealerOrderApprovalOutbox outbox,
        IDealerAccountRepository dealers,
        IPricingService pricing,
        ICustomerLedgerRepository ledger)
    {
        _scope = scope;
        _dealerUsers = dealerUsers;
        _tenant = tenant;
        _currentUser = currentUser;
        _orders = orders;
        _customers = customers;
        _products = products;
        _addresses = addresses;
        _paymentTerms = paymentTerms;
        _sequences = sequences;
        _uow = uow;
        _outbox = outbox;
        _dealers = dealers;
        _pricing = pricing;
        _ledger = ledger;
    }

    public async Task<OrderDto> Handle(CreateDealerOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines is null || request.Lines.Count == 0)
        {
            throw new InvalidOrderLineException("At least one order line is required.");
        }

        var dealerAccountId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var tenantId = _tenant.RequireTenantId();
        var currentUserId = _currentUser.UserIdOrThrow();

        var allowed = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);
        if (!allowed.Contains(request.CustomerId))
        {
            throw new DealerCustomerNotAuthorizedException();
        }

        var dealerUser = await _dealerUsers.GetByUserAndDealerAsync(currentUserId, dealerAccountId, cancellationToken);
        var dealerUserId = dealerUser?.Id;

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }

        var orderNumber = await _sequences.ConsumeAsync(DocumentSequenceType.OrderNumber, DateTime.UtcNow, cancellationToken);
        var currency = !string.IsNullOrWhiteSpace(request.Currency) ? request.Currency : customer.DefaultCurrency;

        var availableAddresses = await _addresses.GetByCustomerAsync(customer.Id, cancellationToken);
        var defaultAddress = availableAddresses.FirstOrDefault(a => a.IsPrimary) ?? availableAddresses.FirstOrDefault();
        var billingAddress = ResolveAddressOrThrow(request.BillingAddressId, availableAddresses, defaultAddress);
        var shippingAddress = ResolveAddressOrThrow(request.ShippingAddressId, availableAddresses, defaultAddress);

        var order = new Order(orderNumber, customer.Id, DateTime.UtcNow, currency, request.Notes);

        order.UpdateDetails(
            type: OrderType.Standard,
            source: OrderSource.Manual,
            requestedDeliveryDate: null,
            promisedDeliveryDate: null,
            billingAddressId: billingAddress?.Id,
            shippingAddressId: shippingAddress?.Id,
            paymentTermsId: customer.PaymentTermsId,
            priceListId: customer.PriceListId,
            exchangeRate: 1m,
            shippingCost: 0m,
            headerDiscountPercent: 0m,
            headerDiscountAmount: 0m,
            salesRepUserId: customer.SalesRepUserId,
            channel: "Dealer",
            internalNotes: null,
            customerNotes: request.CustomerNotes,
            originOrderId: null);

        PaymentTerm? paymentTerm = null;
        if (customer.PaymentTermsId is Guid ptid)
        {
            paymentTerm = await _paymentTerms.GetByIdAsync(ptid, cancellationToken);
        }
        order.ApplySnapshots(
            new CustomerSnapshot
            {
                Code = customer.Code,
                LegalName = customer.LegalName ?? customer.Name,
                TradeName = customer.TradeName,
                TaxNumber = customer.TaxNumber,
                TaxOffice = customer.TaxOffice,
                NationalId = customer.NationalId,
                Email = customer.Email,
                Phone = customer.Phone,
            },
            billingAddressSnapshot: billingAddress is null ? null : ToSnapshot(billingAddress),
            shippingAddressSnapshot: shippingAddress is null ? null : ToSnapshot(shippingAddress),
            paymentTermsNetDays: paymentTerm?.NetDays,
            dueDate: paymentTerm?.ResolveDueDate(DateTime.UtcNow));

        var lineNumber = 1;
        var resolvedLines = new List<OrderLine>(request.Lines.Count);
        foreach (var input in request.Lines)
        {
            var product = products[input.ProductId];

            var minQuantity = await _pricing.ResolveMinQuantityAsync(product.Id, customer.Id, cancellationToken);
            if (minQuantity.HasValue && input.Quantity < minQuantity.Value)
            {
                throw new MinOrderQuantityNotMetException(product.Id, lineNumber, input.Quantity, minQuantity.Value);
            }

            var resolution = await _pricing.ResolveAsync(
                new PriceResolutionRequest(product.Id, customer.Id, input.Quantity, DateTime.UtcNow, currency),
                cancellationToken);

            if (!string.Equals(resolution.Currency, order.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new CurrencyMismatchException(product.Id, order.Currency, resolution.Currency);
            }

            var unitPrice = resolution.UnitPrice;
            var line = new OrderLine(product.Id, product.Sku, product.Name, input.Quantity, unitPrice);
            line.SetLineNumber(lineNumber++);
            line.ApplyPricing(
                input.Quantity,
                resolution.ReferenceListPrice ?? product.ListPrice,
                unitPrice,
                resolution.DiscountPercent,
                lineDiscountAmount: 0m,
                isManualPriceOverride: false,
                resolution.TaxRatePercent,
                resolution.TaxRateId,
                resolution.IsTaxInclusive,
                withholdingRatePercent: 0m,
                product.AverageCost,
                uomId: product.SalesUomId ?? product.BaseUomId,
                uomCode: product.Unit,
                uomConversionFactor: 1m,
                warehouseId: null,
                lineNotes: input.LineNotes,
                null,
                false,
                product.Description);
            resolvedLines.Add(line);
        }

        order.ReplaceLines(resolvedLines);
        order.MarkOrigin(OrderOriginPersona.Dealer, customerUserId: null, dealerAccountId: dealerAccountId, dealerUserId: dealerUserId);

        await EnforceCreditLimitAsync(customer, order.Total, cancellationToken);

        await _orders.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dealer = await _dealers.GetByIdAsync(dealerAccountId, cancellationToken);
        await _outbox.EnqueueSubmittedForApprovalAsync(
            new DealerOrderSubmittedForApprovalPayload(
                OrderId: order.Id,
                TenantId: tenantId,
                CustomerId: customer.Id,
                DealerAccountId: dealerAccountId,
                DealerName: dealer?.Name ?? string.Empty,
                LineCount: order.Lines.Count,
                Total: order.Total,
                Currency: order.Currency,
                DealerUserId: dealerUserId),
            cancellationToken);

        order.Customer = customer;
        return OrderMapper.ToDto(order, dealer?.Name, null);
    }

    private async Task EnforceCreditLimitAsync(Customer customer, decimal orderTotal, CancellationToken cancellationToken)
    {
        if (customer.CreditLimit <= 0m)
        {
            return;
        }
        var ledgerBalance = await _ledger.GetCurrentBalanceAsync(customer.Id, cancellationToken);
        var currentBalance = CreditSnapshotFactory.ResolveCurrentBalance(customer, ledgerBalance);
        var projected = Math.Max(0m, currentBalance) + orderTotal;
        if (projected > customer.CreditLimit)
        {
            throw new CreditLimitExceededException(customer.CreditLimit, projected);
        }
    }

    private static CustomerAddress? ResolveAddressOrThrow(Guid? addressId, IReadOnlyList<CustomerAddress> available, CustomerAddress? fallback)
    {
        if (!addressId.HasValue || addressId.Value == Guid.Empty) return fallback;
        var match = available.FirstOrDefault(a => a.Id == addressId.Value);
        if (match is null)
        {
            throw new CustomerAddressNotFoundException();
        }
        return match;
    }

    private static AddressSnapshot ToSnapshot(CustomerAddress a) => new()
    {
        Label = a.Label,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
    };
}

public class CancelDealerOrderHandler : IRequestHandler<CancelDealerOrderCommand, OrderDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IDealerAccountRepository _dealers;

    public CancelDealerOrderHandler(
        IPortalScopeService scope,
        IOrderRepository orders,
        IUnitOfWork uow,
        IDealerAccountRepository dealers)
    {
        _scope = scope;
        _orders = orders;
        _uow = uow;
        _dealers = dealers;
    }

    public async Task<OrderDto> Handle(CancelDealerOrderCommand request, CancellationToken cancellationToken)
    {
        var dealerId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var order = await _orders.GetWithLinesAsync(request.OrderId, cancellationToken);

        if (order is null || order.OriginDealerAccountId != dealerId)
        {
            throw new OrderNotFoundException();
        }
        if (!order.IsPendingDealerApproval)
        {
            throw new InvalidOrderApprovalStateException(
                "Only orders waiting for customer approval can be cancelled by the dealer.");
        }

        order.Cancel(string.IsNullOrWhiteSpace(request.Reason) ? "Dealer cancelled before approval." : request.Reason);
        await _uow.SaveChangesAsync(cancellationToken);

        var dealer = await _dealers.GetByIdAsync(dealerId, cancellationToken);
        return OrderMapper.ToDto(order, dealer?.Name, null);
    }
}

public class ListDealerCatalogProductsHandler : IRequestHandler<ListDealerCatalogProductsQuery, PagedResult<ProductDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IProductRepository _products;
    private readonly IPricingService _pricing;
    private readonly IDealerCustomerLinkRepository _links;
    private readonly ICustomerDealerProductVisibilityRepository _visibility;

    public ListDealerCatalogProductsHandler(
        IPortalScopeService scope,
        IProductRepository products,
        IPricingService pricing,
        IDealerCustomerLinkRepository links,
        ICustomerDealerProductVisibilityRepository visibility)
    {
        _scope = scope;
        _products = products;
        _pricing = pricing;
        _links = links;
        _visibility = visibility;
    }

    public async Task<PagedResult<ProductDto>> Handle(ListDealerCatalogProductsQuery request, CancellationToken cancellationToken)
    {
        var dealerId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        IReadOnlyCollection<Guid>? restrictToIds = null;
        Guid? scopedCustomerId = null;

        if (request.CustomerId is Guid cid)
        {
            var allowed = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);
            if (allowed.Contains(cid))
            {
                scopedCustomerId = cid;
                var link = await _links.GetByDealerAndCustomerAsync(dealerId, cid, cancellationToken);
                if (link is not null && await _visibility.HasAnyForLinkAsync(link.Id, cancellationToken))
                {
                    var visibleIds = await _visibility.ListVisibleProductIdsAsync(link.Id, cancellationToken);
                    restrictToIds = visibleIds;
                }
            }
        }

        var (items, total) = await _products.SearchAsync(
            request.Search,
            isActive: true,
            page,
            pageSize,
            restrictToIds,
            cancellationToken);

        var dtos = items.Select(MapProductToDto).ToList();

        if (scopedCustomerId is Guid resolvedCustomerId && dtos.Count > 0)
        {
            var requests = dtos
                .Select(d => new PriceResolutionRequest(d.Id, resolvedCustomerId, 1m, DateTime.UtcNow, null))
                .ToList();
            var resolved = await _pricing.ResolveBatchAsync(requests, cancellationToken);
            for (var i = 0; i < dtos.Count && i < resolved.Count; i++)
            {
                dtos[i].Price = resolved[i].UnitPrice;
                if (!string.IsNullOrWhiteSpace(resolved[i].Currency)) dtos[i].Currency = resolved[i].Currency;
            }
        }

        return new PagedResult<ProductDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    private static ProductDto MapProductToDto(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Barcode = p.Barcode,
        Mpn = p.Mpn,
        Name = p.Name,
        ShortDescription = p.ShortDescription,
        Description = p.Description,
        Slug = p.Slug,
        BrandId = p.BrandId,
        CategoryId = p.CategoryId,
        ParentProductId = p.ParentProductId,
        Unit = p.Unit,
        BaseUomId = p.BaseUomId,
        PurchaseUomId = p.PurchaseUomId,
        SalesUomId = p.SalesUomId,
        Price = p.Price,
        ListPrice = p.ListPrice,
        MinSellingPrice = p.MinSellingPrice,
        StandardCost = p.StandardCost,
        LastPurchaseCost = p.LastPurchaseCost,
        AverageCost = p.AverageCost,
        Currency = p.Currency,
        TaxRateId = p.TaxRateId,
        IsPriceTaxInclusive = p.IsPriceTaxInclusive,
        StockQuantity = p.StockQuantity,
        IsStockTracked = p.IsStockTracked,
        IsLotTracked = p.IsLotTracked,
        IsSerialTracked = p.IsSerialTracked,
        MinStock = p.MinStock,
        MaxStock = p.MaxStock,
        ReorderPoint = p.ReorderPoint,
        SafetyStock = p.SafetyStock,
        LeadTimeDays = p.LeadTimeDays,
        WeightKg = p.WeightKg,
        WidthCm = p.WidthCm,
        HeightCm = p.HeightCm,
        DepthCm = p.DepthCm,
        VolumeM3 = p.VolumeM3,
        MinOrderQuantity = p.MinOrderQuantity,
        Status = p.Status,
        LaunchDate = p.LaunchDate,
        EndOfLifeDate = p.EndOfLifeDate,
        IsActive = p.IsActive,
        CreatedAtUtc = p.CreatedAtUtc,
        UpdatedAtUtc = p.UpdatedAtUtc,
    };
}
