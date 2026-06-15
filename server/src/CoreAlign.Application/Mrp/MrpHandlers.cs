using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Application.Fx;
using CoreAlign.Application.Purchasing;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Purchasing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Mrp;

internal static class PurchaseRequisitionMapper
{
    public static PurchaseRequisitionDto ToDto(PurchaseRequisition r) => new(
        r.Id,
        r.Number,
        r.Status,
        r.Reason,
        r.RequestedAtUtc,
        r.RequestedByUserId,
        r.ApprovedByUserId,
        r.ApprovedAtUtc,
        r.SubmittedAtUtc,
        r.RejectedAtUtc,
        r.RejectReason,
        r.CancelledAtUtc,
        r.CancelReason,
        r.ConvertedAtUtc,
        r.ConvertedPurchaseOrderId,
        r.Notes,
        r.Lines.OrderBy(l => l.LineNumber).Select(ToLineDto).ToList(),
        Math.Round(r.Lines.Sum(l => l.EstimatedLineTotal), 4),
        r.CreatedAtUtc,
        r.ConcurrencyToken);

    private static PurchaseRequisitionLineDto ToLineDto(PurchaseRequisitionLine l) => new(
        l.Id,
        l.LineNumber,
        l.ProductId,
        l.ProductSku,
        l.ProductName,
        l.QuantityRequested,
        l.EstimatedUnitCost,
        l.EstimatedLineTotal,
        l.PreferredSupplierId,
        l.ExpectedDeliveryDate,
        l.Notes);
}

public class CreatePurchaseRequisitionHandler : IRequestHandler<CreatePurchaseRequisitionCommand, PurchaseRequisitionDto>
{
    private readonly IPurchaseRequisitionRepository _requisitions;
    private readonly IProductRepository _products;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _uow;

    public CreatePurchaseRequisitionHandler(
        IPurchaseRequisitionRepository requisitions,
        IProductRepository products,
        IDocumentSequenceRepository sequences,
        ICurrentUserAccessor currentUser,
        IUnitOfWork uow)
    {
        _requisitions = requisitions;
        _products = products;
        _sequences = sequences;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<PurchaseRequisitionDto> Handle(CreatePurchaseRequisitionCommand c, CancellationToken ct)
    {
        if (c.Lines.Count == 0)
        {
            throw new InvalidOrderLineException("Requisition must have at least one line.");
        }

        var productIds = c.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, ct);
        if (products.Count != productIds.Count)
        {
            throw new InvalidOrderLineException("One or more products were not found.");
        }

        var now = DateTime.UtcNow;
        await _sequences.EnsureExistsAsync(DocumentSequenceType.PurchaseRequisitionNumber, "PR", 5, now.Year, ct);
        await _uow.SaveChangesAsync(ct);
        var number = await _sequences.ConsumeAsync(DocumentSequenceType.PurchaseRequisitionNumber, now, ct);

        var requisition = new PurchaseRequisition(
            number,
            _currentUser.UserId ?? Guid.Empty,
            c.Reason,
            c.Notes);

        var lines = c.Lines.Select(l =>
        {
            var p = products[l.ProductId];
            return new PurchaseRequisitionLine(
                p.Id,
                p.Sku,
                p.Name,
                l.QuantityRequested,
                l.EstimatedUnitCost,
                l.PreferredSupplierId,
                l.ExpectedDeliveryDate,
                l.Notes);
        }).ToList();
        requisition.ReplaceLines(lines);

        await _requisitions.AddAsync(requisition, ct);
        return PurchaseRequisitionMapper.ToDto(requisition);
    }
}

public class SubmitPurchaseRequisitionHandler : IRequestHandler<SubmitPurchaseRequisitionCommand, PurchaseRequisitionDto>
{
    private readonly IPurchaseRequisitionRepository _requisitions;
    public SubmitPurchaseRequisitionHandler(IPurchaseRequisitionRepository requisitions) => _requisitions = requisitions;

    public async Task<PurchaseRequisitionDto> Handle(SubmitPurchaseRequisitionCommand c, CancellationToken ct)
    {
        var requisition = await _requisitions.GetByIdAsync(c.Id, ct)
            ?? throw new PurchaseRequisitionNotFoundException();
        requisition.Submit();
        _requisitions.Update(requisition);
        return PurchaseRequisitionMapper.ToDto(requisition);
    }
}

public class ApprovePurchaseRequisitionHandler : IRequestHandler<ApprovePurchaseRequisitionCommand, PurchaseRequisitionDto>
{
    private readonly IPurchaseRequisitionRepository _requisitions;
    private readonly ICurrentUserAccessor _currentUser;

    public ApprovePurchaseRequisitionHandler(IPurchaseRequisitionRepository requisitions, ICurrentUserAccessor currentUser)
    {
        _requisitions = requisitions;
        _currentUser = currentUser;
    }

    public async Task<PurchaseRequisitionDto> Handle(ApprovePurchaseRequisitionCommand c, CancellationToken ct)
    {
        var requisition = await _requisitions.GetByIdAsync(c.Id, ct)
            ?? throw new PurchaseRequisitionNotFoundException();
        requisition.Approve(_currentUser.UserId ?? Guid.Empty);
        _requisitions.Update(requisition);
        return PurchaseRequisitionMapper.ToDto(requisition);
    }
}

public class RejectPurchaseRequisitionHandler : IRequestHandler<RejectPurchaseRequisitionCommand, PurchaseRequisitionDto>
{
    private readonly IPurchaseRequisitionRepository _requisitions;
    public RejectPurchaseRequisitionHandler(IPurchaseRequisitionRepository requisitions) => _requisitions = requisitions;

    public async Task<PurchaseRequisitionDto> Handle(RejectPurchaseRequisitionCommand c, CancellationToken ct)
    {
        var requisition = await _requisitions.GetByIdAsync(c.Id, ct)
            ?? throw new PurchaseRequisitionNotFoundException();
        requisition.Reject(c.Reason);
        _requisitions.Update(requisition);
        return PurchaseRequisitionMapper.ToDto(requisition);
    }
}

public class CancelPurchaseRequisitionHandler : IRequestHandler<CancelPurchaseRequisitionCommand, PurchaseRequisitionDto>
{
    private readonly IPurchaseRequisitionRepository _requisitions;
    public CancelPurchaseRequisitionHandler(IPurchaseRequisitionRepository requisitions) => _requisitions = requisitions;

    public async Task<PurchaseRequisitionDto> Handle(CancelPurchaseRequisitionCommand c, CancellationToken ct)
    {
        var requisition = await _requisitions.GetByIdAsync(c.Id, ct)
            ?? throw new PurchaseRequisitionNotFoundException();
        requisition.Cancel(c.Reason);
        _requisitions.Update(requisition);
        return PurchaseRequisitionMapper.ToDto(requisition);
    }
}

public class ConvertRequisitionToPurchaseOrderHandler : IRequestHandler<ConvertRequisitionToPurchaseOrderCommand, Guid>
{
    private const string BaseCurrency = "TRY";

    private readonly IPurchaseRequisitionRepository _requisitions;
    private readonly IVendorRepository _vendors;
    private readonly IProductRepository _products;
    private readonly ITaxRateRepository _taxRates;
    private readonly IMediator _mediator;
    private readonly ILogger<ConvertRequisitionToPurchaseOrderHandler> _logger;
    private readonly IFxRateResolver? _fxResolver;
    private readonly ITenantContext? _tenantContext;

    public ConvertRequisitionToPurchaseOrderHandler(
        IPurchaseRequisitionRepository requisitions,
        IVendorRepository vendors,
        IProductRepository products,
        ITaxRateRepository taxRates,
        IMediator mediator,
        ILogger<ConvertRequisitionToPurchaseOrderHandler> logger,
        IFxRateResolver? fxResolver = null,
        ITenantContext? tenantContext = null)
    {
        _requisitions = requisitions;
        _vendors = vendors;
        _products = products;
        _taxRates = taxRates;
        _mediator = mediator;
        _logger = logger;
        _fxResolver = fxResolver;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> Handle(ConvertRequisitionToPurchaseOrderCommand c, CancellationToken ct)
    {
        var requisition = await _requisitions.GetByIdAsync(c.Id, ct)
            ?? throw new PurchaseRequisitionNotFoundException();
        var vendor = await _vendors.GetByIdAsync(c.VendorId, ct)
            ?? throw new VendorNotFoundForPurchaseException();

        var currency = string.IsNullOrWhiteSpace(c.Currency) ? BaseCurrency : c.Currency.ToUpperInvariant();
        var orderDate = DateTime.UtcNow;

        var taxRatePercentByProductId = await ResolveLineTaxRatesAsync(requisition.Lines, ct);
        var exchangeRate = await ResolveExchangeRateAsync(currency, orderDate, ct);

        var lines = requisition.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new PurchaseOrderLineInput(
                ProductId: l.ProductId,
                Quantity: l.QuantityRequested,
                UnitCost: l.EstimatedUnitCost,
                TaxRatePercent: taxRatePercentByProductId.GetValueOrDefault(l.ProductId, 0m),
                UomId: null,
                UomCode: null,
                LineNotes: l.Notes))
            .ToList();

        var createCommand = new CreatePurchaseOrderCommand(
            VendorId: vendor.Id,
            OrderDate: orderDate,
            Currency: currency,
            Lines: lines,
            PoNumber: null,
            ExpectedDate: c.ExpectedDate,
            ExchangeRate: exchangeRate,
            WarehouseId: null,
            Notes: requisition.Notes);

        var po = await _mediator.Send(createCommand, ct);

        requisition.MarkConverted(po.Id);
        _requisitions.Update(requisition);

        _logger.LogInformation(
            "Purchase requisition {RequisitionId} ({Number}) converted to purchase order {PurchaseOrderId} ({PoNumber}) via mediator pipeline.",
            requisition.Id, requisition.Number, po.Id, po.PoNumber);

        return po.Id;
    }

    private async Task<IReadOnlyDictionary<Guid, decimal>> ResolveLineTaxRatesAsync(
        IEnumerable<PurchaseRequisitionLine> lines,
        CancellationToken ct)
    {
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var products = await _products.GetByIdsAsync(productIds, ct);
        var taxRatePercentByTaxRateId = new Dictionary<Guid, decimal>();
        var result = new Dictionary<Guid, decimal>(productIds.Count);

        foreach (var productId in productIds)
        {
            if (!products.TryGetValue(productId, out var product) || product.TaxRateId is not { } taxRateId)
            {
                result[productId] = 0m;
                continue;
            }

            if (!taxRatePercentByTaxRateId.TryGetValue(taxRateId, out var percent))
            {
                var taxRate = await _taxRates.GetByIdAsync(taxRateId, ct);
                percent = taxRate?.RatePercent ?? 0m;
                taxRatePercentByTaxRateId[taxRateId] = percent;
            }

            result[productId] = percent;
        }

        return result;
    }

    private async Task<decimal> ResolveExchangeRateAsync(string currency, DateTime asOfDate, CancellationToken ct)
    {
        if (_fxResolver is null || string.Equals(currency, BaseCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        try
        {
            var snapshot = await _fxResolver.ResolveAsync(currency, asOfDate, _tenantContext?.CurrentTenantId, ct);
            return snapshot is null || snapshot.BuyingRate <= 0m ? 1m : snapshot.BuyingRate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "FX rate resolution failed for currency {Currency}; falling back to 1.0 for the converted purchase order.",
                currency);
            return 1m;
        }
    }
}

public class GenerateMrpSuggestionsHandler : IRequestHandler<GenerateMrpSuggestionsCommand, MrpSuggestionResultDto>
{
    private readonly IMrpService _mrp;
    public GenerateMrpSuggestionsHandler(IMrpService mrp) => _mrp = mrp;

    public Task<MrpSuggestionResultDto> Handle(GenerateMrpSuggestionsCommand c, CancellationToken ct) =>
        _mrp.GenerateRequisitionSuggestionsAsync(c.AsOfDateUtc ?? DateTime.UtcNow, ct);
}

public class GetMrpDashboardHandler : IRequestHandler<GetMrpDashboardQuery, MrpDashboardDto>
{
    private readonly IMrpService _mrp;
    public GetMrpDashboardHandler(IMrpService mrp) => _mrp = mrp;

    public Task<MrpDashboardDto> Handle(GetMrpDashboardQuery q, CancellationToken ct) =>
        _mrp.GetDashboardAsync(q.TopN, ct);
}

public class GetStockProjectionHandler : IRequestHandler<GetStockProjectionQuery, StockProjectionDto?>
{
    private readonly IMrpService _mrp;
    public GetStockProjectionHandler(IMrpService mrp) => _mrp = mrp;

    public Task<StockProjectionDto?> Handle(GetStockProjectionQuery q, CancellationToken ct) =>
        _mrp.ProjectStockBalanceAsync(q.ProductId, q.DaysAhead, ct);
}

public class GetDemandForecastHandler : IRequestHandler<GetDemandForecastQuery, DemandForecastDto?>
{
    private readonly IMrpService _mrp;
    public GetDemandForecastHandler(IMrpService mrp) => _mrp = mrp;

    public Task<DemandForecastDto?> Handle(GetDemandForecastQuery q, CancellationToken ct) =>
        _mrp.CalculateDemandForecastAsync(q.ProductId, q.WindowDays, ct);
}

public class ListPurchaseRequisitionsHandler : IRequestHandler<ListPurchaseRequisitionsQuery, PagedResult<PurchaseRequisitionDto>>
{
    private readonly IPurchaseRequisitionRepository _requisitions;
    public ListPurchaseRequisitionsHandler(IPurchaseRequisitionRepository requisitions) => _requisitions = requisitions;

    public async Task<PagedResult<PurchaseRequisitionDto>> Handle(ListPurchaseRequisitionsQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _requisitions.SearchAsync(q.Status, q.ProductId, q.FromUtc, q.ToUtc, page, pageSize, ct);
        return new PagedResult<PurchaseRequisitionDto>
        {
            Items = items.Select(PurchaseRequisitionMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}
