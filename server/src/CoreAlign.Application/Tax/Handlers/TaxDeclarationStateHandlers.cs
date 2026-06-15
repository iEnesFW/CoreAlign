using CoreAlign.Application.Tax.Commands;
using CoreAlign.Application.Tax.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tax.Handlers;

public class MarkTaxDeclarationSubmittedCommandHandler
    : IRequestHandler<MarkTaxDeclarationSubmittedCommand, TaxDeclarationDto>
{
    private readonly ITaxDeclarationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkTaxDeclarationSubmittedCommandHandler(
        ITaxDeclarationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaxDeclarationDto> Handle(
        MarkTaxDeclarationSubmittedCommand request,
        CancellationToken cancellationToken)
    {
        var declaration = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new TaxDeclarationNotFoundException();

        declaration.MarkSubmitted(request.SubmittedAtUtc);
        _repository.Update(declaration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TaxDeclarationMapper.ToDto(declaration);
    }
}

public class MarkTaxDeclarationAcceptedCommandHandler
    : IRequestHandler<MarkTaxDeclarationAcceptedCommand, TaxDeclarationDto>
{
    private readonly ITaxDeclarationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkTaxDeclarationAcceptedCommandHandler(
        ITaxDeclarationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaxDeclarationDto> Handle(
        MarkTaxDeclarationAcceptedCommand request,
        CancellationToken cancellationToken)
    {
        var declaration = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new TaxDeclarationNotFoundException();

        declaration.MarkAccepted();
        _repository.Update(declaration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TaxDeclarationMapper.ToDto(declaration);
    }
}

public class MarkTaxDeclarationRejectedCommandHandler
    : IRequestHandler<MarkTaxDeclarationRejectedCommand, TaxDeclarationDto>
{
    private readonly ITaxDeclarationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkTaxDeclarationRejectedCommandHandler(
        ITaxDeclarationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaxDeclarationDto> Handle(
        MarkTaxDeclarationRejectedCommand request,
        CancellationToken cancellationToken)
    {
        var declaration = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new TaxDeclarationNotFoundException();

        declaration.MarkRejected(request.Reason);
        _repository.Update(declaration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TaxDeclarationMapper.ToDto(declaration);
    }
}
