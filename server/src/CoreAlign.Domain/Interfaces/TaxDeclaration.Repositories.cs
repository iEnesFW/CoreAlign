using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface ITaxDeclarationRepository
{
    Task<TaxDeclaration?> GetByPeriodAsync(
        int year,
        int month,
        TaxDeclarationType declarationType,
        CancellationToken cancellationToken = default);

    Task<TaxDeclaration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxDeclaration>> ListAsync(
        int? year,
        TaxDeclarationType? declarationType,
        CancellationToken cancellationToken = default);

    Task AddAsync(TaxDeclaration declaration, CancellationToken cancellationToken = default);

    void Update(TaxDeclaration declaration);
}

public record InvoiceTaxAggregateRow(
    Guid InvoiceId,
    decimal TaxableTotal,
    decimal TaxTotal,
    decimal WithholdingTotal,
    string? TaxBreakdownJson);

public record CustomerInvoiceAggregateRow(
    Guid CustomerId,
    string CustomerName,
    string? TaxNumber,
    int DocumentCount,
    decimal TotalAmount,
    decimal TaxAmount);

public record VendorBillAggregateRow(
    Guid VendorId,
    string VendorName,
    string? TaxNumber,
    int DocumentCount,
    decimal TotalAmount,
    decimal TaxAmount);

public interface ITaxAggregationRepository
{
    Task<IReadOnlyList<InvoiceTaxAggregateRow>> GetInvoiceTaxRowsForPeriodAsync(
        DateTime startUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerInvoiceAggregateRow>> GetCustomerInvoiceAggregatesAsync(
        DateTime startUtc,
        DateTime endExclusiveUtc,
        decimal minThreshold,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorBillAggregateRow>> GetVendorBillAggregatesAsync(
        DateTime startUtc,
        DateTime endExclusiveUtc,
        decimal minThreshold,
        CancellationToken cancellationToken = default);
}
