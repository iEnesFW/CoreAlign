using CoreAlign.Application.Quotes.Commands;
using CoreAlign.Application.Quotes.DTOs;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Quotes.Handlers;

public class SendQuoteCommandHandler : IRequestHandler<SendQuoteCommand, QuoteDto>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SendQuoteCommandHandler(IQuoteRepository quoteRepository, IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<QuoteDto> Handle(SendQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new QuoteNotFoundException();

        quote.MarkSent();
        _quoteRepository.Update(quote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return QuoteMapper.ToDto(quote);
    }
}

public class AcceptQuoteCommandHandler : IRequestHandler<AcceptQuoteCommand, QuoteDto>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptQuoteCommandHandler(IQuoteRepository quoteRepository, IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<QuoteDto> Handle(AcceptQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new QuoteNotFoundException();

        quote.Accept();
        _quoteRepository.Update(quote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return QuoteMapper.ToDto(quote);
    }
}

public class RejectQuoteCommandHandler : IRequestHandler<RejectQuoteCommand, QuoteDto>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectQuoteCommandHandler(IQuoteRepository quoteRepository, IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<QuoteDto> Handle(RejectQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new QuoteNotFoundException();

        quote.Reject(request.Reason);
        _quoteRepository.Update(quote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return QuoteMapper.ToDto(quote);
    }
}

public class DeleteQuoteCommandHandler : IRequestHandler<DeleteQuoteCommand, bool>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteQuoteCommandHandler(IQuoteRepository quoteRepository, IUnitOfWork unitOfWork)
    {
        _quoteRepository = quoteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new QuoteNotFoundException();

        if (!quote.IsDraft)
        {
            throw new QuoteImmutableException(quote.Status.ToString());
        }

        _quoteRepository.Remove(quote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
