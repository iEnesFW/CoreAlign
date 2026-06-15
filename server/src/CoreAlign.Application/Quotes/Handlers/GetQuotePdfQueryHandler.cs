using CoreAlign.Application.Documents;
using CoreAlign.Application.Quotes.Queries;
using MediatR;

namespace CoreAlign.Application.Quotes.Handlers;

public class GetQuotePdfQueryHandler : IRequestHandler<GetQuotePdfQuery, QuotePdfResult>
{
    private readonly IDocumentService _documentService;

    public GetQuotePdfQueryHandler(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<QuotePdfResult> Handle(GetQuotePdfQuery request, CancellationToken cancellationToken)
    {
        var doc = await _documentService.RenderQuotePdfAsync(request.Id, cancellationToken);
        return new QuotePdfResult(doc.Content, doc.FileName, doc.ContentType);
    }
}
