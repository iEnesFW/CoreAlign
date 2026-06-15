using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Documents;

public sealed class DocumentService : IDocumentService
{
    private readonly IDocumentRenderer _renderer;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IPaymentTermRepository _paymentTermRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IDealerAccountRepository _dealerAccountRepository;
    private readonly IDealerCommissionLedgerRepository _commissionRepository;
    private readonly ITenantContext _tenantContext;

    public DocumentService(
        IDocumentRenderer renderer,
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IShipmentRepository shipmentRepository,
        ICustomerRepository customerRepository,
        ITenantRepository tenantRepository,
        IWarehouseRepository warehouseRepository,
        IPaymentTermRepository paymentTermRepository,
        IQuoteRepository quoteRepository,
        IDealerAccountRepository dealerAccountRepository,
        IDealerCommissionLedgerRepository commissionRepository,
        ITenantContext tenantContext)
    {
        _renderer = renderer;
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
        _shipmentRepository = shipmentRepository;
        _customerRepository = customerRepository;
        _tenantRepository = tenantRepository;
        _warehouseRepository = warehouseRepository;
        _paymentTermRepository = paymentTermRepository;
        _quoteRepository = quoteRepository;
        _dealerAccountRepository = dealerAccountRepository;
        _commissionRepository = commissionRepository;
        _tenantContext = tenantContext;
    }

    public async Task<DocumentResult> RenderQuotePdfAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _quoteRepository.GetWithLinesAsync(quoteId, cancellationToken)
            ?? throw new QuoteNotFoundException();
        _tenantContext.EnsureSameTenant(quote.TenantId);

        var tenant = await LoadTenantAsync(quote.TenantId, cancellationToken);
        var customer = await _customerRepository.GetByIdAsync(quote.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        var paymentTerm = quote.PaymentTermsId.HasValue
            ? await _paymentTermRepository.GetByIdAsync(quote.PaymentTermsId.Value, cancellationToken)
            : null;

        var model = quote.ToQuoteDocumentModel(tenant, customer, paymentTerm);
        var bytes = await _renderer.RenderQuoteAsync(model, cancellationToken);
        return new DocumentResult(bytes, BuildFileName("Quote", model.QuoteNumber, model.Tenant.TenantSlug));
    }

    public async Task<DocumentResult> RenderInvoicePdfAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var model = await BuildInvoiceModelAsync(invoiceId, cancellationToken);
        var bytes = await _renderer.RenderInvoiceAsync(model, cancellationToken);
        return new DocumentResult(bytes, BuildFileName("Invoice", model.DocumentNumber, model.Tenant.TenantSlug));
    }

    public async Task<DocumentResult> RenderCreditNotePdfAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var model = await BuildInvoiceModelAsync(invoiceId, cancellationToken);
        var bytes = await _renderer.RenderCreditNoteAsync(model, cancellationToken);
        return new DocumentResult(bytes, BuildFileName("CreditNote", model.DocumentNumber, model.Tenant.TenantSlug));
    }

    public async Task<DocumentResult> RenderOrderPdfAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetWithLinesAsync(orderId, cancellationToken)
            ?? throw new OrderNotFoundException();
        _tenantContext.EnsureSameTenant(order.TenantId);
        var tenant = await LoadTenantAsync(order.TenantId, cancellationToken);
        var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        var paymentTerm = order.PaymentTermsId.HasValue
            ? await _paymentTermRepository.GetByIdAsync(order.PaymentTermsId.Value, cancellationToken)
            : null;

        var model = order.ToOrderDocumentModel(tenant, customer, paymentTerm);
        var bytes = await _renderer.RenderOrderConfirmationAsync(model, cancellationToken);
        return new DocumentResult(bytes, BuildFileName("Order", model.OrderNumber, model.Tenant.TenantSlug));
    }

    public async Task<DocumentResult> RenderShipmentPdfAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetWithLinesAsync(shipmentId, cancellationToken)
            ?? throw new ShipmentNotFoundException();
        _tenantContext.EnsureSameTenant(shipment.TenantId);

        var order = await _orderRepository.GetByIdAsync(shipment.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();
        var tenant = await LoadTenantAsync(shipment.TenantId, cancellationToken);
        var customer = await _customerRepository.GetByIdAsync(shipment.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        var warehouse = await _warehouseRepository.GetByIdAsync(shipment.WarehouseId, cancellationToken);

        var model = shipment.ToShipmentDocumentModel(order, tenant, customer, warehouse);
        var bytes = await _renderer.RenderPackingSlipAsync(model, cancellationToken);
        return new DocumentResult(bytes, BuildFileName("PackingSlip", model.ShipmentNumber, model.Tenant.TenantSlug));
    }

    public async Task<DocumentResult> RenderInvoicePdfForCustomerAsync(Guid invoiceId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetWithLinesAsync(invoiceId, cancellationToken);
        if (invoice is null || invoice.CustomerId != customerId)
        {
            throw new InvoiceNotFoundException();
        }
        return await RenderInvoicePdfAsync(invoiceId, cancellationToken);
    }

    public async Task<DocumentResult> RenderOrderPdfForCustomerAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.CustomerId != customerId)
        {
            throw new OrderNotFoundException();
        }
        return await RenderOrderPdfAsync(orderId, cancellationToken);
    }

    public async Task<DocumentResult> RenderOrderPdfForDealerAsync(Guid orderId, Guid dealerAccountId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.OriginDealerAccountId != dealerAccountId)
        {
            throw new OrderNotFoundException();
        }
        return await RenderOrderPdfAsync(orderId, cancellationToken);
    }

    public async Task<DocumentResult> RenderInvoicePdfForDealerAsync(
        Guid invoiceId,
        Guid dealerAccountId,
        IReadOnlyCollection<Guid> allowedCustomerIds,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetWithLinesAsync(invoiceId, cancellationToken);
        if (invoice is null || !allowedCustomerIds.Contains(invoice.CustomerId))
        {
            throw new InvoiceNotFoundException();
        }
        _ = dealerAccountId;
        return await RenderInvoicePdfAsync(invoiceId, cancellationToken);
    }

    public async Task<DocumentResult> RenderDealerCommissionStatementPdfAsync(
        Guid dealerAccountId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var dealer = await _dealerAccountRepository.GetByIdAsync(dealerAccountId, cancellationToken)
            ?? throw new DealerAccountNotFoundException();
        _tenantContext.EnsureSameTenant(dealer.TenantId);

        var tenant = await LoadTenantAsync(dealer.TenantId, cancellationToken);
        var entries = await _commissionRepository.ListForStatementAsync(dealerAccountId, fromUtc, toUtc, cancellationToken);

        var orderIds = entries.Select(e => e.OrderId).Distinct().ToList();
        var orders = new Dictionary<Guid, Domain.Entities.Order>();
        foreach (var oid in orderIds)
        {
            var ord = await _orderRepository.GetByIdAsync(oid, cancellationToken);
            if (ord is not null) orders[oid] = ord;
        }
        var customerIds = entries.Select(e => e.CustomerId).Distinct().ToList();
        var customers = new Dictionary<Guid, Domain.Entities.Customer>();
        foreach (var cid in customerIds)
        {
            var cust = await _customerRepository.GetByIdAsync(cid, cancellationToken);
            if (cust is not null) customers[cid] = cust;
        }

        var shipmentIds = entries
            .Where(e => e.ShipmentId.HasValue)
            .Select(e => e.ShipmentId!.Value)
            .Distinct()
            .ToList();
        var shipments = new Dictionary<Guid, Domain.Entities.Shipment>();
        foreach (var sid in shipmentIds)
        {
            var sh = await _shipmentRepository.GetByIdAsync(sid, cancellationToken);
            if (sh is not null) shipments[sid] = sh;
        }

        var lines = entries.Select(e => new DealerCommissionStatementLine(
            AccruedAtUtc: e.AccruedAtUtc,
            OrderNumber: orders.TryGetValue(e.OrderId, out var o) ? o.OrderNumber : e.OrderId.ToString(),
            ShipmentNumber: e.ShipmentId.HasValue && shipments.TryGetValue(e.ShipmentId.Value, out var s) ? s.ShipmentNumber : null,
            CustomerName: customers.TryGetValue(e.CustomerId, out var c) ? c.Name : string.Empty,
            OrderTotal: e.OrderTotal,
            CommissionPercent: e.CommissionPercent,
            CommissionAmount: e.CommissionAmount,
            Status: e.Status.ToString())).ToList();

        var totalAccrued = Math.Round(entries.Where(e => e.Status != Domain.Enums.DealerCommissionStatus.Cancelled).Sum(e => e.CommissionAmount), 4);
        var totalPaid = Math.Round(entries.Where(e => e.Status == Domain.Enums.DealerCommissionStatus.Paid).Sum(e => e.CommissionAmount), 4);
        var totalOutstanding = Math.Round(totalAccrued - totalPaid, 4);

        var currency = entries
            .GroupBy(e => e.Currency)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "TRY";

        var model = new DealerCommissionStatementModel(
            DocumentTitle: "Commission Statement / Komisyon Ekstresi",
            DealerName: dealer.Name,
            DealerCode: dealer.Code,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Currency: currency,
            Tenant: tenant.ToHeader(),
            Lines: lines,
            TotalAccrued: totalAccrued,
            TotalPaid: totalPaid,
            TotalOutstanding: totalOutstanding);

        var bytes = await _renderer.RenderDealerCommissionStatementAsync(model, cancellationToken);
        var fileNumber = $"{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}";
        return new DocumentResult(bytes, BuildFileName("CommissionStatement", $"{dealer.Code}-{fileNumber}", tenant.Slug));
    }

    private async Task<InvoiceDocumentModel> BuildInvoiceModelAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetWithLinesAsync(invoiceId, cancellationToken)
            ?? throw new InvoiceNotFoundException();
        _tenantContext.EnsureSameTenant(invoice.TenantId);

        var tenant = await LoadTenantAsync(invoice.TenantId, cancellationToken);
        var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        var paymentTerm = invoice.PaymentTermsId.HasValue
            ? await _paymentTermRepository.GetByIdAsync(invoice.PaymentTermsId.Value, cancellationToken)
            : null;

        return invoice.ToInvoiceDocumentModel(tenant, customer, paymentTerm);
    }

    private async Task<Domain.Entities.Tenant> LoadTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new MissingTenantContextException();
    }

    private static string BuildFileName(string docType, string documentNumber, string tenantSlug)
    {
        var safeNumber = Sanitize(documentNumber);
        var safeSlug = Sanitize(tenantSlug);
        return $"{docType}-{safeNumber}-{safeSlug}.pdf";
    }

    private static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "doc";
        var chars = input.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-').ToArray();
        var s = new string(chars);
        while (s.Contains("--", StringComparison.Ordinal))
        {
            s = s.Replace("--", "-");
        }
        return s.Trim('-');
    }
}
