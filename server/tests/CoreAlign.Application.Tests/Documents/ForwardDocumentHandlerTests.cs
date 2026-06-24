using CoreAlign.Application.B2B;
using CoreAlign.Application.Documents;
using CoreAlign.Application.Documents.Forwarding;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Documents;

public class ForwardDocumentHandlerTests
{
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDocumentService _documents = Substitute.For<IDocumentService>();
    private readonly IForwardDocumentService _service = Substitute.For<IForwardDocumentService>();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _documentId = Guid.NewGuid();

    private ForwardCustomerDocumentHandler BuildSut() =>
        new(_tenantContext, _scope, _currentUser, _users, _documents, _service);

    private void StubBaseScope()
    {
        _tenantContext.RequireTenantId().Returns(_tenantId);
        _currentUser.UserIdOrThrow().Returns(_userId);
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(_customerId);
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new User(_tenantId, "dealer", "dealer@example.com", "hash") { Id = _userId });
    }

    private ForwardCustomerDocumentCommand Command() =>
        new(ForwardableDocumentType.Invoice, _documentId, "external@example.com", Guid.NewGuid());

    [Fact]
    public async Task Cross_scope_document_render_blocks_forward_idor_guard()
    {
        StubBaseScope();
        _documents.RenderInvoicePdfForCustomerAsync(_documentId, _customerId, Arg.Any<CancellationToken>())
            .Returns<Task<DocumentResult>>(_ => throw new InvoiceNotFoundException());

        var act = async () => await BuildSut().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<InvoiceNotFoundException>();
        await _service.DidNotReceive().ForwardAsync(Arg.Any<ForwardDocumentContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rate_limit_blocks_before_rendering()
    {
        StubBaseScope();
        _service.EnsureWithinLimitAsync(_tenantId, _userId, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new DocumentForwardRateLimitExceededException());

        var act = async () => await BuildSut().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<DocumentForwardRateLimitExceededException>();
        await _documents.DidNotReceive().RenderInvoicePdfForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _service.DidNotReceive().ForwardAsync(Arg.Any<ForwardDocumentContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Happy_path_forwards_with_reply_to_sender_and_scoped_pdf()
    {
        StubBaseScope();
        var pdf = new DocumentResult(new byte[] { 1, 2, 3 }, "INV-100.pdf");
        _documents.RenderInvoicePdfForCustomerAsync(_documentId, _customerId, Arg.Any<CancellationToken>()).Returns(pdf);
        _service.ForwardAsync(Arg.Any<ForwardDocumentContext>(), Arg.Any<CancellationToken>())
            .Returns(new ForwardDocumentResult(true, "Queued"));

        var result = await BuildSut().Handle(Command(), CancellationToken.None);

        result.Queued.Should().BeTrue();
        await _service.Received(1).ForwardAsync(
            Arg.Is<ForwardDocumentContext>(c =>
                c.TenantId == _tenantId &&
                c.RecipientEmail == "external@example.com" &&
                c.ReplyToEmail == "dealer@example.com" &&
                c.CustomerId == _customerId &&
                c.Pdf == pdf),
            Arg.Any<CancellationToken>());
    }
}
