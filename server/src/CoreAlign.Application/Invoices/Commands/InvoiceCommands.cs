using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using MediatR;

namespace CoreAlign.Application.Invoices.Commands;

public record GenerateInvoiceFromOrderCommand(Guid OrderId, int DueDays = 30, string? Notes = null)
    : IRequest<InvoiceDto>, ITransactionalRequest;

public record MarkInvoiceAsPaidCommand(Guid Id) : IRequest<InvoiceDto>, ITransactionalRequest;

public record CancelInvoiceCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
