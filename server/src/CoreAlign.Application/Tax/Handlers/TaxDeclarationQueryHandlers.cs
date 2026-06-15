using CoreAlign.Application.Tax.Commands;
using CoreAlign.Application.Tax.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tax.Handlers;

public class GetTaxDeclarationByIdQueryHandler : IRequestHandler<GetTaxDeclarationByIdQuery, TaxDeclarationDto>
{
    private readonly ITaxDeclarationRepository _repository;

    public GetTaxDeclarationByIdQueryHandler(ITaxDeclarationRepository repository)
    {
        _repository = repository;
    }

    public async Task<TaxDeclarationDto> Handle(GetTaxDeclarationByIdQuery request, CancellationToken cancellationToken)
    {
        var declaration = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new TaxDeclarationNotFoundException();
        return TaxDeclarationMapper.ToDto(declaration);
    }
}

public class ListTaxDeclarationsQueryHandler
    : IRequestHandler<ListTaxDeclarationsQuery, IReadOnlyList<TaxDeclarationSummaryDto>>
{
    private readonly ITaxDeclarationRepository _repository;

    public ListTaxDeclarationsQueryHandler(ITaxDeclarationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TaxDeclarationSummaryDto>> Handle(
        ListTaxDeclarationsQuery request,
        CancellationToken cancellationToken)
    {
        var declarations = await _repository.ListAsync(request.Year, request.DeclarationType, cancellationToken);
        return declarations.Select(TaxDeclarationMapper.ToSummaryDto).ToList();
    }
}

public class GetTaxDeclarationXmlQueryHandler : IRequestHandler<GetTaxDeclarationXmlQuery, string>
{
    private readonly ITaxDeclarationRepository _repository;

    public GetTaxDeclarationXmlQueryHandler(ITaxDeclarationRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> Handle(GetTaxDeclarationXmlQuery request, CancellationToken cancellationToken)
    {
        var declaration = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new TaxDeclarationNotFoundException();

        return declaration.XmlPayload ?? string.Empty;
    }
}
