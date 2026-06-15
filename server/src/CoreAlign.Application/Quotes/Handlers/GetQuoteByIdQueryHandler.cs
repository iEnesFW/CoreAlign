using CoreAlign.Application.Quotes.DTOs;
using CoreAlign.Application.Quotes.Queries;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Quotes.Handlers;

public class GetQuoteByIdQueryHandler : IRequestHandler<GetQuoteByIdQuery, QuoteDto>
{
    private readonly IQuoteRepository _quoteRepository;

    public GetQuoteByIdQueryHandler(IQuoteRepository quoteRepository)
    {
        _quoteRepository = quoteRepository;
    }

    public async Task<QuoteDto> Handle(GetQuoteByIdQuery request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new QuoteNotFoundException();
        return QuoteMapper.ToDto(quote);
    }
}
