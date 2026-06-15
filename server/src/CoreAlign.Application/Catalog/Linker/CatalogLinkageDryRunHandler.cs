using MediatR;

namespace CoreAlign.Application.Catalog.Linker;

public sealed class CatalogLinkageDryRunHandler : IRequestHandler<CatalogLinkageDryRunCommand, LinkageReport>
{
    private readonly ICatalogProductLinker _linker;

    public CatalogLinkageDryRunHandler(ICatalogProductLinker linker) => _linker = linker;

    public Task<LinkageReport> Handle(CatalogLinkageDryRunCommand request, CancellationToken cancellationToken) =>
        _linker.RunDryRunAsync(cancellationToken);
}
