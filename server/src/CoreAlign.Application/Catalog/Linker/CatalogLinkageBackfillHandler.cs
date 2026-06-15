using MediatR;

namespace CoreAlign.Application.Catalog.Linker;

public sealed class CatalogLinkageBackfillHandler : IRequestHandler<CatalogLinkageBackfillCommand, int>
{
    private readonly ICatalogProductLinker _linker;

    public CatalogLinkageBackfillHandler(ICatalogProductLinker linker) => _linker = linker;

    public Task<int> Handle(CatalogLinkageBackfillCommand request, CancellationToken cancellationToken) =>
        _linker.BackfillAllAsync(cancellationToken);
}
