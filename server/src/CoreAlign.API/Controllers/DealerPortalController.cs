using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerPortal;
using CoreAlign.Application.CustomerPortal.Credit;
using CoreAlign.Application.Documents;
using CoreAlign.Application.Orders.Revisions;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

/// <summary>
/// Endpoints powering the Dealer (B2B) Portal SPA. Every action auto-scopes to
/// the caller's dealer via <see cref="IPortalScopeService.GetCurrentDealerAccountIdAsync"/>;
/// non-dealer personas receive HTTP 403 from the global exception middleware via
/// <see cref="Domain.Exceptions.PortalScopeNotResolvedException"/>. Detail endpoints
/// additionally compare the loaded order's <c>OriginDealerAccountId</c> to the
/// resolved id and respond with HTTP 404 on mismatch — equivalent to not-found,
/// so the response never reveals that a resource belongs to another dealer in
/// the same tenant.
/// </summary>
[ApiController]
[Authorize(Policy = PersonaPolicies.Dealer)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dealer-portal")]
public class DealerPortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPortalScopeService _portalScope;
    private readonly IDocumentService _documents;

    public DealerPortalController(IMediator mediator, IPortalScopeService portalScope, IDocumentService documents)
    {
        _mediator = mediator;
        _portalScope = portalScope;
        _documents = documents;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => (await _mediator.Send(new GetDealerPortalDashboardQuery(), ct)).ToOk();

    /// <summary>
    /// Customers the current dealer is authorized to act on behalf of. The list
    /// is derived from active <c>DealerCustomerLink</c> rows server-side and
    /// cannot be influenced by request parameters.
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetAllowedCustomers(CancellationToken ct)
        => (await _mediator.Send(new ListDealerAllowedCustomersQuery(), ct)).ToOk();

    /// <summary>
    /// Orders this dealer has submitted (filterable by status and approval status).
    /// Backed by an index on <c>(TenantId, OriginDealerAccountId, CreatedAtUtc DESC)</c>.
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] string? approvalStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListDealerOrdersQuery(status, approvalStatus, page, pageSize), ct)).ToOk();

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetDealerOrderByIdQuery(id), ct)).ToOk();

    [HttpGet("orders/{id:guid}/revisions")]
    public async Task<IActionResult> GetOrderRevisions(Guid id, CancellationToken ct)
    {
        await _portalScope.GetCurrentDealerAccountIdAsync(ct);
        return (await _mediator.Send(new GetOrderRevisionsQuery(id), ct)).ToOk();
    }

    [HttpPost("orders/{id:guid}/revisions")]
    public async Task<IActionResult> RequestOrderRevision(
        Guid id,
        [FromBody] DealerRequestRevisionRequest body,
        CancellationToken ct)
    {
        await _portalScope.GetCurrentDealerAccountIdAsync(ct);
        var cmd = new RequestOrderRevisionCommand(id, body?.ProposedLines ?? Array.Empty<RevisionLineInput>(), body?.RequestNotes);
        return (await _mediator.Send(cmd, ct)).ToCreated();
    }

    [HttpPost("orders/{id:guid}/revisions/{revisionId:guid}/approve")]
    public async Task<IActionResult> ApproveOrderRevision(Guid id, Guid revisionId, CancellationToken ct)
    {
        await _portalScope.GetCurrentDealerAccountIdAsync(ct);
        return (await _mediator.Send(new ApproveOrderRevisionCommand(id, revisionId), ct)).ToOk();
    }

    [HttpPost("orders/{id:guid}/revisions/{revisionId:guid}/reject")]
    public async Task<IActionResult> RejectOrderRevision(
        Guid id,
        Guid revisionId,
        [FromBody] DealerRejectRevisionRequest body,
        CancellationToken ct)
    {
        await _portalScope.GetCurrentDealerAccountIdAsync(ct);
        return (await _mediator.Send(new RejectOrderRevisionCommand(id, revisionId, body?.Reason ?? string.Empty), ct)).ToOk();
    }

    [HttpPost("orders/{id:guid}/revisions/{revisionId:guid}/cancel")]
    public async Task<IActionResult> CancelOrderRevision(Guid id, Guid revisionId, CancellationToken ct)
    {
        await _portalScope.GetCurrentDealerAccountIdAsync(ct);
        return (await _mediator.Send(new CancelOrderRevisionCommand(id, revisionId), ct)).ToOk();
    }

    /// <summary>
    /// Creates an order on behalf of one of the dealer's authorized customers.
    /// The order starts in Draft with <c>DealerApprovalStatus=PendingCustomerApproval</c>
    /// and is gated by the customer's approval before progressing to Submitted.
    /// </summary>
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateDealerOrderCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToCreated();

    /// <summary>
    /// Dealer may cancel their own order ONLY while it is still pending customer
    /// approval. Once approved or rejected, the cancel flow belongs to tenant staff.
    /// </summary>
    [HttpPost("orders/{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelDealerOrderRequest? body, CancellationToken ct)
        => (await _mediator.Send(new CancelDealerOrderCommand(id, body?.Reason), ct)).ToOk();

    /// <summary>
    /// Slim catalog projection for the dealer's product picker. When the
    /// optional <paramref name="customerId"/> is provided AND belongs to one of
    /// the dealer's authorized customers, prices are resolved via the pricing
    /// service so customer-specific price lists / discounts surface in the UI.
    /// </summary>
    [HttpGet("catalog/products")]
    public async Task<IActionResult> GetCatalogProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListDealerCatalogProductsQuery(search, customerId, page, pageSize), ct)).ToOk();

    [HttpGet("orders/{orderId:guid}/comments")]
    public async Task<IActionResult> ListOrderComments(Guid orderId, CancellationToken ct)
        => (await _mediator.Send(new ListDealerPortalOrderCommentsQuery(orderId), ct)).ToOk();

    [HttpPost("orders/{orderId:guid}/comments")]
    public async Task<IActionResult> PostOrderComment(
        Guid orderId,
        [FromBody] PostDealerOrderCommentRequest body,
        CancellationToken ct)
        => (await _mediator.Send(new PostDealerPortalOrderCommentCommand(orderId, body?.Body ?? string.Empty), ct)).ToCreated();

    [HttpGet("orders/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadOrderPdf(Guid id, CancellationToken ct)
    {
        var dealerAccountId = await _portalScope.GetCurrentDealerAccountIdAsync(ct);
        var doc = await _documents.RenderOrderPdfForDealerAsync(id, dealerAccountId, ct);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{doc.FileName}\"";
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListDealerPortalInvoicesQuery(customerId, status, fromUtc, toUtc, page, pageSize), ct)).ToOk();

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetDealerPortalInvoiceByIdQuery(id), ct)).ToOk();

    [HttpGet("invoices/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id, CancellationToken ct)
    {
        var dealerAccountId = await _portalScope.GetCurrentDealerAccountIdAsync(ct);
        var allowed = await _portalScope.GetDealerAllowedCustomerIdsAsync(ct);
        var doc = await _documents.RenderInvoicePdfForDealerAsync(id, dealerAccountId, allowed, ct);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{doc.FileName}\"";
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpGet("commissions")]
    public async Task<IActionResult> GetCommissions(
        [FromQuery] DealerCommissionStatus? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListDealerCommissionsQuery(status, fromUtc, toUtc, page, pageSize), ct)).ToOk();

    [HttpGet("commissions/summary")]
    public async Task<IActionResult> GetCommissionSummary(CancellationToken ct)
        => (await _mediator.Send(new GetDealerCommissionSummaryQuery(), ct)).ToOk();

    [HttpGet("commissions/statement/pdf")]
    public async Task<IActionResult> DownloadCommissionStatement(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken ct)
    {
        var doc = await _mediator.Send(new DownloadDealerCommissionStatementQuery(fromUtc, toUtc), ct);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{doc.FileName}\"";
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => (await _mediator.Send(new GetDealerPortalProfileQuery(), ct)).ToOk();

    [HttpGet("customers/{id:guid}/credit")]
    public async Task<IActionResult> GetCustomerCredit(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetDealerCustomerCreditSnapshotQuery(id), ct)).ToOk();
}

public record CancelDealerOrderRequest(string? Reason);

public record DealerRequestRevisionRequest(IReadOnlyList<RevisionLineInput> ProposedLines, string? RequestNotes);

public record DealerRejectRevisionRequest(string Reason);

public record PostDealerOrderCommentRequest(string Body);
