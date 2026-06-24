using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Common.Observability;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Observability;

public sealed record ReportClientErrorCommand(
    string Message,
    ErrorSeverity Severity,
    string? Page,
    string? Component,
    string? StackTrace,
    string? CorrelationId,
    string? UserAgent,
    string? ContextJson) : IRequest<Unit>;

public sealed class ReportClientErrorCommandValidator : AbstractValidator<ReportClientErrorCommand>
{
    public ReportClientErrorCommandValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.Page).MaximumLength(512);
        RuleFor(x => x.Component).MaximumLength(256);
        RuleFor(x => x.StackTrace).MaximumLength(16000);
        RuleFor(x => x.ContextJson).MaximumLength(16000);
        RuleFor(x => x.UserAgent).MaximumLength(512);
    }
}

public sealed class ReportClientErrorHandler : IRequestHandler<ReportClientErrorCommand, Unit>
{
    private readonly IErrorLogWriter _writer;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IAuditFieldRedactor _redactor;

    public ReportClientErrorHandler(
        IErrorLogWriter writer,
        ITenantContext tenantContext,
        ICurrentUserAccessor currentUser,
        IAuditFieldRedactor redactor)
    {
        _writer = writer;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _redactor = redactor;
    }

    public async Task<Unit> Handle(ReportClientErrorCommand request, CancellationToken cancellationToken)
    {
        var record = new ErrorLogRecord(
            CorrelationId: string.IsNullOrWhiteSpace(request.CorrelationId) ? "client" : request.CorrelationId,
            Source: ErrorSource.Frontend,
            Severity: request.Severity,
            Message: request.Message,
            Path: request.Page,
            ClientPage: request.Page,
            ClientComponent: request.Component,
            StackTrace: request.StackTrace,
            UserAgent: request.UserAgent,
            ContextJson: _redactor.RedactJson(request.ContextJson),
            TenantId: _tenantContext.CurrentTenantId,
            UserId: _currentUser.UserId);

        await _writer.WriteAsync(record, cancellationToken);
        return Unit.Value;
    }
}
