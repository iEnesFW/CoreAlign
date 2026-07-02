using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Invoices.Recurring.Commands;

public record CreateRecurringInvoiceTemplateCommand(
    string Name,
    Guid CustomerId,
    string Currency,
    RecurrenceFrequency Frequency,
    int IntervalCount,
    int? AnchorDayOfMonth,
    DayOfWeek? AnchorDayOfWeek,
    DateOnly StartDate,
    DateOnly? EndDate,
    int? MaxOccurrences,
    IReadOnlyList<RecurringInvoiceLineInput> Lines,
    int DueDays = 30,
    Guid? PaymentTermsId = null,
    decimal? HeaderDiscountPercent = null,
    decimal? HeaderDiscountAmount = null,
    decimal? ShippingCost = null,
    decimal? RoundingAdjustment = null,
    bool AutoConfirm = true,
    string? PublicNotes = null,
    string? InternalNotes = null
) : IRequest<RecurringInvoiceTemplateDto>, ITransactionalRequest;

public record UpdateRecurringInvoiceTemplateCommand(
    Guid Id,
    string Name,
    Guid CustomerId,
    string Currency,
    RecurrenceFrequency Frequency,
    int IntervalCount,
    int? AnchorDayOfMonth,
    DayOfWeek? AnchorDayOfWeek,
    DateOnly StartDate,
    DateOnly? EndDate,
    int? MaxOccurrences,
    IReadOnlyList<RecurringInvoiceLineInput> Lines,
    int DueDays = 30,
    Guid? PaymentTermsId = null,
    decimal? HeaderDiscountPercent = null,
    decimal? HeaderDiscountAmount = null,
    decimal? ShippingCost = null,
    decimal? RoundingAdjustment = null,
    bool AutoConfirm = true,
    string? PublicNotes = null,
    string? InternalNotes = null
) : IRequest<RecurringInvoiceTemplateDto>, ITransactionalRequest;

public record PauseRecurringInvoiceTemplateCommand(Guid Id) : IRequest<RecurringInvoiceTemplateDto>, ITransactionalRequest;

public record ResumeRecurringInvoiceTemplateCommand(Guid Id) : IRequest<RecurringInvoiceTemplateDto>, ITransactionalRequest;

public record CancelRecurringInvoiceTemplateCommand(Guid Id) : IRequest<RecurringInvoiceTemplateDto>, ITransactionalRequest;

public record RunRecurringInvoiceNowCommand(Guid Id, bool FromJob = false) : IRequest<Guid?>, ITransactionalRequest;
