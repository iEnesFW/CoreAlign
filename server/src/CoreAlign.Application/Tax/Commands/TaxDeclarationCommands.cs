using CoreAlign.Application.Common;
using CoreAlign.Application.Tax.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Tax.Commands;

public record BuildKdv1ForPeriodCommand(int Year, int Month) : IRequest<Guid>, ITransactionalRequest;

public record BuildBaBsForPeriodCommand(int Year, int Month) : IRequest<Guid>, ITransactionalRequest;

public record MarkTaxDeclarationSubmittedCommand(Guid Id, DateTime? SubmittedAtUtc)
    : IRequest<TaxDeclarationDto>, ITransactionalRequest;

public record MarkTaxDeclarationAcceptedCommand(Guid Id) : IRequest<TaxDeclarationDto>, ITransactionalRequest;

public record MarkTaxDeclarationRejectedCommand(Guid Id, string Reason)
    : IRequest<TaxDeclarationDto>, ITransactionalRequest;

public record GetTaxDeclarationByIdQuery(Guid Id) : IRequest<TaxDeclarationDto>;

public record ListTaxDeclarationsQuery(int? Year, TaxDeclarationType? DeclarationType)
    : IRequest<IReadOnlyList<TaxDeclarationSummaryDto>>;

public record GetTaxDeclarationXmlQuery(Guid Id) : IRequest<string>;
