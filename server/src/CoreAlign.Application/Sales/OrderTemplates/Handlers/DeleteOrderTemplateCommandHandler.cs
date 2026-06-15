using CoreAlign.Application.Sales.OrderTemplates.Commands;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Sales.OrderTemplates.Handlers;

public class DeleteOrderTemplateCommandHandler : IRequestHandler<DeleteOrderTemplateCommand, bool>
{
    private readonly IOrderTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOrderTemplateCommandHandler(IOrderTemplateRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteOrderTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new OrderTemplateNotFoundException();
        _repository.Remove(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class SetOrderTemplateActiveCommandHandler : IRequestHandler<SetOrderTemplateActiveCommand, OrderTemplates.DTOs.OrderTemplateDto>
{
    private readonly IOrderTemplateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetOrderTemplateActiveCommandHandler(IOrderTemplateRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderTemplates.DTOs.OrderTemplateDto> Handle(SetOrderTemplateActiveCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new OrderTemplateNotFoundException();
        template.SetActive(request.IsActive);
        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderTemplateMapper.ToDto(template);
    }
}
