using CoreAlign.Application.B2B;
using CoreAlign.Application.Returns.Commands;
using CoreAlign.Application.Returns.DTOs;
using CoreAlign.Application.Returns.Mapping;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Returns.Handlers;

public class CreateReturnRequestCommandHandler : IRequestHandler<CreateReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReturnRequestCommandHandler(
        IReturnRequestRepository returnRepository,
        IOrderRepository orderRepository,
        IDocumentSequenceRepository sequenceRepository,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _returnRepository = returnRepository;
        _orderRepository = orderRepository;
        _sequenceRepository = sequenceRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReturnRequestDto> Handle(CreateReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetWithLinesAsync(request.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        var inputByLine = request.Lines
            .GroupBy(l => l.OrderLineId)
            .ToDictionary(g => g.Key, g => (Qty: g.Sum(x => x.QuantityReturned), Sample: g.First()));

        var orderLinesById = order.Lines.ToDictionary(l => l.Id);

        await _sequenceRepository.EnsureExistsAsync(
            DocumentSequenceType.ReturnRequestNumber, "RMA", 6, DateTime.UtcNow.Year, cancellationToken);
        var number = await _sequenceRepository.ConsumeAsync(
            DocumentSequenceType.ReturnRequestNumber, DateTime.UtcNow, cancellationToken);

        var entity = new ReturnRequest(
            number,
            order,
            request.Reason,
            request.ReasonText,
            requestedByUserId: _currentUser.UserId,
            request.SourceInvoiceId,
            request.CustomerNotes);

        var lines = new List<ReturnRequestLine>();
        foreach (var (lineId, payload) in inputByLine)
        {
            if (!orderLinesById.TryGetValue(lineId, out var orderLine))
            {
                throw new InvalidReturnRequestStateException(
                    $"Order line {lineId} does not belong to order {order.OrderNumber}.");
            }
            lines.Add(new ReturnRequestLine(
                orderLine,
                payload.Qty,
                payload.Sample.Restockable,
                payload.Sample.LineNotes));
        }
        entity.ReplaceLines(lines);
        if (!string.IsNullOrWhiteSpace(request.InternalNotes))
        {
            entity.SetInternalNotes(request.InternalNotes);
        }

        await _returnRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        entity.Order = order;
        return ReturnRequestMapper.ToDto(entity, orderNumber: order.OrderNumber);
    }
}
