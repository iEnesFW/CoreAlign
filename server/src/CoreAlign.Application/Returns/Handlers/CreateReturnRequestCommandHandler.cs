using CoreAlign.Application.B2B;
using CoreAlign.Application.Returns;
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

        // WHY the open requests are subtracted here: OrderLine.QuantityReturned only advances when
        // the goods are RECEIVED, so a second request raised while the first is still open sees the
        // full shipped quantity as returnable. Both would then be received, putting the stock away
        // twice and reversing COGS twice. Rejected/Cancelled requests release their claim; a
        // Received one is already inside QuantityReturned and must not be counted again.
        var claimed = ReturnClaims.ByOrderLine(await _returnRepository.GetByOrderAsync(order.Id, cancellationToken));
        foreach (var (lineId, input) in inputByLine)
        {
            var orderLine = orderLinesById[lineId];
            var remaining = Math.Max(
                0m,
                orderLine.QuantityShipped - orderLine.QuantityReturned - claimed.GetValueOrDefault(lineId));
            if (input.Qty > remaining)
            {
                throw new ReturnExceedsShippedException(orderLine.ProductSku, remaining, input.Qty);
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
