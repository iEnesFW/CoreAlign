using CoreAlign.Application.Common;
using CoreAlign.Application.Returns.DTOs;
using CoreAlign.Application.Returns.Mapping;
using CoreAlign.Application.Returns.Queries;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Returns.Handlers;

public class GetReturnRequestByIdQueryHandler : IRequestHandler<GetReturnRequestByIdQuery, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _repository;
    private readonly IInvoiceRepository _invoiceRepository;

    public GetReturnRequestByIdQueryHandler(
        IReturnRequestRepository repository,
        IInvoiceRepository invoiceRepository)
    {
        _repository = repository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<ReturnRequestDto> Handle(GetReturnRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new ReturnRequestNotFoundException();
        string? sourceInvoiceNumber = null;
        string? creditNoteNumber = null;
        if (entity.SourceInvoiceId.HasValue)
        {
            var src = await _invoiceRepository.GetByIdAsync(entity.SourceInvoiceId.Value, cancellationToken);
            sourceInvoiceNumber = src?.InvoiceNumber;
        }
        if (entity.CreditNoteId.HasValue)
        {
            var cn = await _invoiceRepository.GetByIdAsync(entity.CreditNoteId.Value, cancellationToken);
            creditNoteNumber = cn?.InvoiceNumber;
        }
        return ReturnRequestMapper.ToDto(entity, sourceInvoiceNumber: sourceInvoiceNumber, creditNoteNumber: creditNoteNumber);
    }
}

public class GetReturnRequestsQueryHandler
    : IRequestHandler<GetReturnRequestsQuery, PagedResult<ReturnRequestSummaryDto>>
{
    private readonly IReturnRequestRepository _repository;

    public GetReturnRequestsQueryHandler(IReturnRequestRepository repository) => _repository = repository;

    public async Task<PagedResult<ReturnRequestSummaryDto>> Handle(
        GetReturnRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.SearchAsync(
            request.Search, request.CustomerId, request.OrderId, request.Status,
            request.Page, request.PageSize, cancellationToken);
        return new PagedResult<ReturnRequestSummaryDto>
        {
            Items = items.Select(ReturnRequestMapper.ToSummary).ToList(),
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}

public class GetReturnRequestsByOrderQueryHandler
    : IRequestHandler<GetReturnRequestsByOrderQuery, List<ReturnRequestSummaryDto>>
{
    private readonly IReturnRequestRepository _repository;

    public GetReturnRequestsByOrderQueryHandler(IReturnRequestRepository repository) => _repository = repository;

    public async Task<List<ReturnRequestSummaryDto>> Handle(
        GetReturnRequestsByOrderQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await _repository.GetByOrderAsync(request.OrderId, cancellationToken);
        return entities.Select(ReturnRequestMapper.ToSummary).ToList();
    }
}
