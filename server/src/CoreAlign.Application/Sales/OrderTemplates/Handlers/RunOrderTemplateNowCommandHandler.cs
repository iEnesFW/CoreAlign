using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Sales.OrderTemplates.Commands;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Sales.OrderTemplates.Handlers;

public class RunOrderTemplateNowCommandHandler : IRequestHandler<RunOrderTemplateNowCommand, Guid>
{
    private readonly IOrderTemplateRepository _repository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public RunOrderTemplateNowCommandHandler(
        IOrderTemplateRepository repository,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(RunOrderTemplateNowCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new OrderTemplateNotFoundException();

        var now = DateTime.UtcNow;
        var orderId = await RecurringOrderRunner.RunOnceAsync(template, _mediator, now, cancellationToken);
        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return orderId;
    }
}

internal static class RecurringOrderRunner
{
    public static async Task<Guid> RunOnceAsync(
        Domain.Entities.Sales.OrderTemplate template,
        IMediator mediator,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (template.Lines.Count == 0)
        {
            throw new InvalidOrderLineException("Order template has no lines.");
        }

        var lines = template.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new OrderLineInput(
                ProductId: l.ProductId,
                Quantity: l.Quantity,
                UnitPrice: l.UnitPrice))
            .ToList();

        var command = new CreateOrderCommand(
            OrderNumber: string.Empty,
            CustomerId: template.CustomerId,
            OrderDate: nowUtc,
            Currency: template.Currency,
            Notes: template.Notes,
            Lines: lines,
            PriceListId: template.PriceListId);

        var dto = await mediator.Send(command, cancellationToken);
        template.RecordRun(nowUtc);
        return dto.Id;
    }
}
