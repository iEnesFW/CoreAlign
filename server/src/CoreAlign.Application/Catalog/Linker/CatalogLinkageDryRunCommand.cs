using MediatR;

namespace CoreAlign.Application.Catalog.Linker;

public sealed record CatalogLinkageDryRunCommand() : IRequest<LinkageReport>;
