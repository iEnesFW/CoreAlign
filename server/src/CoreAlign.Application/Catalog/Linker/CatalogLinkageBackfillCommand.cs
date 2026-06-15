using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Catalog.Linker;

public sealed record CatalogLinkageBackfillCommand() : IRequest<int>, ITransactionalRequest;
