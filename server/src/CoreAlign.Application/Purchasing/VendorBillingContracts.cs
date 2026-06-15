using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Purchasing;

public record VendorBillDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    string BillNumber,
    DateTime BillDate,
    DateTime? DueDate,
    string Currency,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    decimal AmountPaid,
    decimal AmountDue,
    VendorBillStatus Status,
    Guid? PurchaseOrderId,
    string? Notes,
    DateTime CreatedAtUtc);

public record VendorPaymentDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    string PaymentNumber,
    DateTime PaymentDate,
    decimal Amount,
    decimal AppliedAmount,
    decimal UnappliedAmount,
    bool IsVoided,
    DateTime? VoidedAtUtc,
    string? VoidReason,
    string Currency,
    string? Method,
    Guid? VendorBillId,
    string? Notes,
    DateTime CreatedAtUtc);

public record VendorPaymentApplicationDto(
    Guid Id,
    Guid VendorPaymentId,
    string PaymentNumber,
    Guid VendorBillId,
    string BillNumber,
    decimal AppliedAmount,
    DateTime AppliedAtUtc,
    Guid? AppliedByUserId,
    string? Notes);

public record CreateVendorBillCommand(
    Guid VendorId,
    string BillNumber,
    DateTime BillDate,
    string Currency,
    decimal Subtotal,
    decimal TaxAmount,
    DateTime? DueDate = null,
    decimal ExchangeRate = 1m,
    Guid? PurchaseOrderId = null,
    string? Notes = null) : IRequest<VendorBillDto>, ITransactionalRequest;

public record PostVendorBillCommand(Guid Id) : IRequest<VendorBillDto>, ITransactionalRequest;
public record CancelVendorBillCommand(Guid Id) : IRequest<VendorBillDto>, ITransactionalRequest;

public record CreateVendorPaymentCommand(
    Guid VendorId,
    decimal Amount,
    DateTime PaymentDate,
    string Currency,
    string? Method = null,
    Guid? VendorBillId = null,
    decimal ExchangeRate = 1m,
    string? Notes = null) : IRequest<VendorPaymentDto>, ITransactionalRequest;

public record UpdateVendorBillCommand(
    Guid Id,
    string BillNumber,
    DateTime BillDate,
    string Currency,
    decimal Subtotal,
    decimal TaxAmount,
    DateTime? DueDate = null,
    decimal ExchangeRate = 1m,
    Guid? PurchaseOrderId = null,
    string? Notes = null) : IRequest<VendorBillDto>, ITransactionalRequest;

public record UpdateVendorPaymentCommand(
    Guid Id,
    DateTime PaymentDate,
    decimal Amount,
    string Currency,
    decimal ExchangeRate = 1m,
    string? Method = null,
    string? Notes = null) : IRequest<VendorPaymentDto>, ITransactionalRequest;

public record VoidVendorPaymentCommand(Guid Id, string? Reason = null) : IRequest<VendorPaymentDto>, ITransactionalRequest;

public record ApplyVendorPaymentCommand(
    Guid VendorPaymentId,
    Guid VendorBillId,
    decimal Amount,
    string? Notes = null) : IRequest<VendorPaymentApplicationDto>, ITransactionalRequest;

public record SearchVendorBillsQuery(Guid? VendorId, VendorBillStatus? Status, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<VendorBillDto>>;
public record GetVendorBillByIdQuery(Guid Id) : IRequest<VendorBillDto?>;
public record GetVendorBillApplicationsQuery(Guid VendorBillId) : IRequest<IReadOnlyList<VendorPaymentApplicationDto>>;
public record GetVendorPaymentByIdQuery(Guid Id) : IRequest<VendorPaymentDto?>;
public record GetVendorPaymentApplicationsQuery(Guid VendorPaymentId) : IRequest<IReadOnlyList<VendorPaymentApplicationDto>>;
public record SearchVendorPaymentsQuery(Guid? VendorId, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<VendorPaymentDto>>;

public record ThreeWayMatchRowDto(
    Guid PurchaseOrderId,
    string PoNumber,
    Guid VendorId,
    string VendorName,
    string Currency,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal ExpectedQty,
    decimal ReceivedQty,
    decimal BilledQty,
    decimal ExpectedAmount,
    decimal BilledAmount,
    IReadOnlyList<string> Discrepancies);

public record GetThreeWayMatchQuery(
    Guid? VendorId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IRequest<IReadOnlyList<ThreeWayMatchRowDto>>;

public record VendorAgingRowDto(
    Guid VendorId,
    string VendorName,
    string Currency,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal DaysOver90,
    decimal Total);

public record GetVendorAgingQuery(DateTime? AsOfUtc = null) : IRequest<IReadOnlyList<VendorAgingRowDto>>;
