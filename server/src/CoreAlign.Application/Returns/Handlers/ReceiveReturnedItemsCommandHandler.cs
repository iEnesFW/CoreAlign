using CoreAlign.Application.B2B;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Returns.Commands;
using CoreAlign.Application.Returns.DTOs;
using CoreAlign.Application.Returns.Mapping;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Returns.Handlers;

public class ReceiveReturnedItemsCommandHandler : IRequestHandler<ReceiveReturnedItemsCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _repository;
    private readonly IOrderRepository _orderRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ReceiveReturnedItemsCommandHandler(
        IReturnRequestRepository repository,
        IOrderRepository orderRepository,
        IWarehouseRepository warehouseRepository,
        IInvoiceRepository invoiceRepository,
        IMediator mediator,
        ITenantContext tenantContext,
        ICurrentUserAccessor currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _orderRepository = orderRepository;
        _warehouseRepository = warehouseRepository;
        _invoiceRepository = invoiceRepository;
        _mediator = mediator;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReturnRequestDto> Handle(ReceiveReturnedItemsCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new ReturnRequestNotFoundException();
        _tenantContext.EnsureSameTenant(entity.TenantId);

        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new InvalidReturnRequestStateException("Warehouse not found.");

        var order = await _orderRepository.GetWithLinesAsync(entity.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException();

        var receivedByUserId = _currentUser.UserIdOrThrow();
        entity.MarkReceived(receivedByUserId, warehouse.Id);

        foreach (var line in entity.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == line.OrderLineId)
                ?? throw new InvalidReturnRequestStateException(
                    $"Order line {line.OrderLineId} not found on order {order.OrderNumber}.");
            orderLine.RecordReturn(line.QuantityReturned);
        }
        _orderRepository.Update(order);
        _repository.Update(entity);

        if (request.AutoIssueCreditNote && entity.SourceInvoiceId.HasValue)
        {
            await IssueAndAttachCreditNoteAsync(entity, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ReturnRequestMapper.ToDto(entity, orderNumber: order.OrderNumber);
    }

    private async Task IssueAndAttachCreditNoteAsync(
        Domain.Entities.ReturnRequest entity,
        CancellationToken cancellationToken)
    {
        var sourceInvoice = await _invoiceRepository.GetWithLinesAsync(
            entity.SourceInvoiceId!.Value, cancellationToken)
            ?? throw new InvoiceNotFoundException();

        var byOrderLine = sourceInvoice.Lines
            .Where(il => il.OriginOrderLineId.HasValue)
            .GroupBy(il => il.OriginOrderLineId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var creditLines = new List<IssueCreditNoteLineInput>();
        foreach (var rl in entity.Lines)
        {
            if (!byOrderLine.TryGetValue(rl.OrderLineId, out var invLine))
            {
                throw new CannotIssueCreditNoteException(
                    $"Cannot match return line for order line {rl.OrderLineId} against invoice {sourceInvoice.InvoiceNumber}.");
            }
            creditLines.Add(new IssueCreditNoteLineInput(invLine.Id, rl.QuantityReturned));
        }

        var creditNoteDto = await _mediator.Send(new IssueCreditNoteCommand(
            sourceInvoice.Id, creditLines, $"RMA {entity.ReturnNumber}", entity.Id), cancellationToken);
        entity.AttachCreditNote(creditNoteDto.Id);
    }
}
