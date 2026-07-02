using CoreAlign.Application.Invoices.Recurring.Commands;
using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Recurring.Handlers;

public class PauseRecurringInvoiceTemplateCommandHandler
    : IRequestHandler<PauseRecurringInvoiceTemplateCommand, RecurringInvoiceTemplateDto>
{
    private readonly IRecurringInvoiceTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PauseRecurringInvoiceTemplateCommandHandler(
        IRecurringInvoiceTemplateRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringInvoiceTemplateDto> Handle(
        PauseRecurringInvoiceTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new RecurringInvoiceTemplateNotFoundException();
        template.Pause();
        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RecurringInvoiceMapper.ToDto(template);
    }
}

public class ResumeRecurringInvoiceTemplateCommandHandler
    : IRequestHandler<ResumeRecurringInvoiceTemplateCommand, RecurringInvoiceTemplateDto>
{
    private readonly IRecurringInvoiceTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ResumeRecurringInvoiceTemplateCommandHandler(
        IRecurringInvoiceTemplateRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringInvoiceTemplateDto> Handle(
        ResumeRecurringInvoiceTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new RecurringInvoiceTemplateNotFoundException();
        template.Resume(DateOnly.FromDateTime(DateTime.UtcNow));
        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RecurringInvoiceMapper.ToDto(template);
    }
}

public class CancelRecurringInvoiceTemplateCommandHandler
    : IRequestHandler<CancelRecurringInvoiceTemplateCommand, RecurringInvoiceTemplateDto>
{
    private readonly IRecurringInvoiceTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelRecurringInvoiceTemplateCommandHandler(
        IRecurringInvoiceTemplateRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecurringInvoiceTemplateDto> Handle(
        CancelRecurringInvoiceTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new RecurringInvoiceTemplateNotFoundException();
        template.Cancel();
        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return RecurringInvoiceMapper.ToDto(template);
    }
}
