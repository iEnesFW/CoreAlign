using CoreAlign.Application.Common;
using CoreAlign.Application.Quotes.DTOs;
using CoreAlign.Application.Quotes.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Quotes.Handlers;

public class GetQuotesQueryHandler : IRequestHandler<GetQuotesQuery, PagedResult<QuoteSummaryDto>>
{
    private readonly IQuoteRepository _quoteRepository;

    public GetQuotesQueryHandler(IQuoteRepository quoteRepository)
    {
        _quoteRepository = quoteRepository;
    }

    public async Task<PagedResult<QuoteSummaryDto>> Handle(GetQuotesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _quoteRepository.SearchAsync(
            request.Search,
            request.CustomerId,
            request.Status,
            page,
            pageSize,
            cancellationToken);

        var dtos = items.Select(QuoteMapper.ToSummaryDto).ToList();

        return new PagedResult<QuoteSummaryDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}
