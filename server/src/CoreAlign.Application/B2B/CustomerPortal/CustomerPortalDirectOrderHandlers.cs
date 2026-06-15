using CoreAlign.Application.Common;
using CoreAlign.Application.CustomerPortal.Credit;
using CoreAlign.Application.Products.DTOs;
using CoreAlign.Application.Products.Mapping;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B.CustomerPortal;

public class CreateCustomerDirectOrderHandler : IRequestHandler<CreateCustomerDirectOrderCommand, Guid>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IPaymentTermRepository _paymentTerms;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IUnitOfWork _uow;
    private readonly IPricingService _pricing;
    private readonly ICustomerAddressRepository _addresses;
    private readonly ICustomerLedgerRepository _ledger;

    public CreateCustomerDirectOrderHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IOrderRepository orders,
        ICustomerRepository customers,
        IProductRepository products,
        IPaymentTermRepository paymentTerms,
        IDocumentSequenceRepository sequences,
        IUnitOfWork uow,
        IPricingService pricing,
        ICustomerAddressRepository addresses,
        ICustomerLedgerRepository ledger)
    {
        _scope = scope;
        _currentUser = currentUser;
        _orders = orders;
        _customers = customers;
        _products = products;
        _paymentTerms = paymentTerms;
        _sequences = sequences;
        _uow = uow;
        _pricing = pricing;
        _addresses = addresses;
        _ledger = ledger;
    }

    public async Task<Guid> Handle(CreateCustomerDirectOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines is null || request.Lines.Count == 0)
        {
            throw new InvalidOrderLineException("At least one order line is required.");
        }

        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var currentUserId = _currentUser.UserIdOrThrow();

        var customer = await _customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }

        var orderNumber = await _sequences.ConsumeAsync(DocumentSequenceType.OrderNumber, DateTime.UtcNow, cancellationToken);
        var currency = customer.DefaultCurrency;

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
            channel: "CustomerPortal",
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
        order.MarkOrigin(OrderOriginPersona.Customer, customerUserId: currentUserId, dealerAccountId: null, dealerUserId: null);

        await EnforceCreditLimitAsync(customer, order.Total, cancellationToken);

        order.Submit();

        await _orders.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return order.Id;
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

public class ListCustomerCatalogProductsHandler : IRequestHandler<ListCustomerCatalogProductsQuery, PagedResult<ProductDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IProductRepository _products;
    private readonly IPricingService _pricing;

    public ListCustomerCatalogProductsHandler(
        IPortalScopeService scope,
        IProductRepository products,
        IPricingService pricing)
    {
        _scope = scope;
        _products = products;
        _pricing = pricing;
    }

    public async Task<PagedResult<ProductDto>> Handle(ListCustomerCatalogProductsQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _products.SearchAsync(request.Search, isActive: true, page, pageSize, cancellationToken);

        var dtos = items.Select(ProductMapper.ToDto).ToList();

        if (dtos.Count > 0)
        {
            var requests = dtos
                .Select(d => new PriceResolutionRequest(d.Id, customerId, 1m, DateTime.UtcNow, null))
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
}
