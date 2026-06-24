using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Payroll.GL;

public record PayPayrollTaxesCommand(
    Guid PaymentId,
    decimal Amount,
    DateTime PaymentDate,
    string Reference,
    bool FromCash = false) : IRequest<Unit>, ITransactionalRequest;

public record PayPayrollSgkCommand(
    Guid PaymentId,
    decimal Amount,
    DateTime PaymentDate,
    string Reference,
    bool FromCash = false) : IRequest<Unit>, ITransactionalRequest;
