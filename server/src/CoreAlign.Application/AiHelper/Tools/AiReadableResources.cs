using System;
using System.Collections.Generic;
using System.Linq;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Application.Inventory.StockCounts;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Application.Orders.Queries;
using CoreAlign.Application.Payments.Queries;
using CoreAlign.Application.Payroll.Employees;
using CoreAlign.Application.Payroll.Runs;
using CoreAlign.Application.Products.Queries;
using CoreAlign.Application.Purchasing;
using CoreAlign.Application.Quotes.Queries;
using CoreAlign.Application.Returns.Queries;
using CoreAlign.Application.Shipments.Queries;
using CoreAlign.Application.Tax.Commands;
using CoreAlign.Application.Vendors.Queries;
using CoreAlign.Application.Warranty;

namespace CoreAlign.Application.AiHelper.Tools;

public sealed record AiReadableResource(
    string Name,
    string Description,
    Func<Guid, object>? DetailQuery,
    Func<string, int, object>? SearchQuery,
    Func<Guid, object>? PortalDetailQuery = null);

public interface IAiReadableResourceRegistry
{
    IReadOnlyList<AiReadableResource> All { get; }

    AiReadableResource? Resolve(string? name);
}

public sealed class AiReadableResourceRegistry : IAiReadableResourceRegistry
{
    private readonly IReadOnlyDictionary<string, AiReadableResource> _byName;

    public AiReadableResourceRegistry()
    {
        All = AiReadableResources.All;
        _byName = All.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AiReadableResource> All { get; }

    public AiReadableResource? Resolve(string? name) =>
        !string.IsNullOrWhiteSpace(name) && _byName.TryGetValue(name, out var r) ? r : null;
}

public static class AiReadableResources
{
    public static readonly IReadOnlyList<AiReadableResource> All = new List<AiReadableResource>
    {
        new("order", "A sales order: line items (qty, unit price, discounts), header discount, tax, shipping, rounding, total, currency, status. Explain a total or analyze lines.",
            id => new GetOrderByIdQuery(id), (search, take) => new GetOrdersQuery(1, take, search),
            id => new GetCustomerPortalOrderByIdQuery(id)),
        new("invoice", "An invoice: line items, discounts, tax (VAT/withholding), shipping, totals, status, payment state.",
            id => new GetInvoiceByIdQuery(id), (search, take) => new GetInvoicesQuery(1, take, search),
            id => new GetCustomerPortalInvoiceByIdQuery(id)),
        new("quote", "A sales quote with its lines, totals and status.",
            id => new GetQuoteByIdQuery(id), null),
        new("customer", "A customer: contact, addresses, balance, credit limit, status.",
            id => new GetCustomerByIdQuery(id), (search, take) => new GetCustomersQuery(1, take, search)),
        new("product", "A product/catalog item: SKU, pricing, stock quantity, attributes.",
            id => new GetProductByIdQuery(id), (search, take) => new GetProductsQuery(1, take, search)),
        new("payment", "A customer payment: amount, currency, status, applications to invoices.",
            id => new GetPaymentByIdQuery(id), null),
        new("vendor", "A vendor/supplier: contact, terms, balance.",
            id => new GetVendorByIdQuery(id), null),
        new("vendor_bill", "A vendor bill (AP): lines, tax, totals, status, 3-way-match state.",
            id => new GetVendorBillByIdQuery(id), null),
        new("vendor_payment", "A vendor payment: amount, status, applications to bills.",
            id => new GetVendorPaymentByIdQuery(id), null),
        new("purchase_order", "A purchase order: lines, quantities ordered/received/billed, totals, status.",
            id => new GetPurchaseOrderByIdQuery(id), null),
        new("goods_receipt", "A goods receipt (GRN): received lines, quantities, costs, reversal state.",
            id => new GetGoodsReceiptByIdQuery(id), null),
        new("return", "A return request: returned lines, reason, status, linked credit note.",
            id => new GetReturnRequestByIdQuery(id), null),
        new("shipment", "A shipment: dispatched lines, tracking, status.",
            id => new GetShipmentByIdQuery(id), null),
        new("stock_count", "A stock/cycle count: counted vs system quantities, variances, status.",
            id => new GetStockCountByIdQuery(id), null),
        new("gl_account", "A general-ledger account (TDHP): code, name, type, balances.",
            id => new GetGLAccountByIdQuery(id), null),
        new("journal_entry", "A journal entry: debit/credit lines, status, source document.",
            id => new GetJournalEntryByIdQuery(id), null),
        new("accounting_period", "An accounting period: range, open/closed/locked status.",
            id => new GetAccountingPeriodByIdQuery(id), null),
        new("tax_declaration", "A tax declaration: period, computed amounts, status.",
            id => new GetTaxDeclarationByIdQuery(id), null),
        new("employee", "An employee: HR/payroll profile (PII is masked).",
            id => new GetEmployeeByIdQuery(id), null),
        new("payroll_run", "A payroll run: period, totals, status, payslips.",
            id => new GetPayrollRunByIdQuery(id), null),
        new("payslip", "A payslip: gross/net, deductions, employer cost (PII masked).",
            id => new GetPayslipByIdQuery(id), null),
        new("glass_project", "A glass-enclosure project: panels, configuration, status.",
            id => new GetGlassProjectByIdQuery(id), null),
        new("field_survey", "A glass field survey: measurements, photos, approval status.",
            id => new GetFieldSurveyByIdQuery(id), null),
        new("warranty_contract", "A warranty contract: coverage, dates, status.",
            id => new GetWarrantyContractByIdQuery(id), null),
        new("warehouse", "A warehouse/location.",
            id => new GetWarehouseByIdQuery(id), null),
        new("price_list", "A price list with its items.",
            id => new GetPriceListByIdQuery(id), null),
    };
}
