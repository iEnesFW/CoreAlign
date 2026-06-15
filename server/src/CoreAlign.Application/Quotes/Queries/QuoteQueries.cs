using CoreAlign.Application.Common;
using CoreAlign.Application.Quotes.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Quotes.Queries;

public record GetQuotesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CustomerId = null,
    QuoteStatus? Status = null) : IRequest<PagedResult<QuoteSummaryDto>>;

public record GetQuoteByIdQuery(Guid Id) : IRequest<QuoteDto>;

public record GetQuotePdfQuery(Guid Id) : IRequest<QuotePdfResult>;

public sealed record QuotePdfResult(byte[] Content, string FileName, string ContentType);
