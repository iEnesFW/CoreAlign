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

        // WHY: uygunluk + satır-üyelik kontrolleri belge-sırası TÜKETİMİNDEN ÖNCE — aksi halde reddedilecek
        // istek RMA numarasını boşa harcar ve sequence hatası domain guard'ını maskeleyip 500'e dönüşür.
        if (!IsReturnableOrderStatus(order.Status))
        {
            throw new InvalidReturnRequestStateException(
                $"Return cannot be created for an order in status '{order.Status}'.");
        }

        var inputByLine = request.Lines
            .GroupBy(l => l.OrderLineId)
            .ToDictionary(g => g.Key, g => (Qty: g.Sum(x => x.QuantityReturned), Sample: g.First()));

        var orderLinesById = order.Lines.ToDictionary(l => l.Id);
        foreach (var lineId in inputByLine.Keys)
        {
            if (!orderLinesById.ContainsKey(lineId))
            {
                throw new InvalidReturnRequestStateException(
                    $"Order line {lineId} does not belong to order {order.OrderNumber}.");
            }
        }

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

        var lines = inputByLine
            .Select(kvp => new ReturnRequestLine(
                orderLinesById[kvp.Key],
                kvp.Value.Qty,
                kvp.Value.Sample.Restockable,
                kvp.Value.Sample.LineNotes))
            .ToList();
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

    private static bool IsReturnableOrderStatus(OrderStatus status) => status
        is OrderStatus.Shipped
        or OrderStatus.PartiallyShipped
        or OrderStatus.Delivered
        or OrderStatus.Closed
        or OrderStatus.Returned;
}
