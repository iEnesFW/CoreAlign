using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.CustomerPortal;
using CoreAlign.Application.CustomerPortal.Addresses;
using CoreAlign.Application.CustomerPortal.Credit;
using CoreAlign.Application.CustomerPortal.Notifications;
using CoreAlign.Application.CustomerPortal.Payments;
using CoreAlign.Application.CustomerPortal.Profile;
using CoreAlign.Application.Customers.Statements;
using CoreAlign.Application.Documents;
using CoreAlign.Application.Orders.Revisions;
using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Customer)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-portal")]
public class CustomerPortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPortalScopeService _portalScope;
    private readonly IDocumentService _documents;
    private readonly IReportFileFactory _reportFileFactory;
    private readonly ITenantRepository _tenants;
    private readonly ITenantContext _tenantContext;

    public CustomerPortalController(
        IMediator mediator,
        IPortalScopeService portalScope,
        IDocumentService documents,
        IReportFileFactory reportFileFactory,
        ITenantRepository tenants,
        ITenantContext tenantContext)
    {
        _mediator = mediator;
        _portalScope = portalScope;
        _documents = documents;
        _reportFileFactory = reportFileFactory;
        _tenants = tenants;
        _tenantContext = tenantContext;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => (await _mediator.Send(new GetCustomerPortalDashboardQuery(), ct)).ToOk();

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetCustomerPortalOrdersQuery(status, page, pageSize), ct)).ToOk();

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetCustomerPortalOrderByIdQuery(id), ct)).ToOk();

    [HttpGet("orders/{id:guid}/revisions")]
    public async Task<IActionResult> GetOrderRevisions(Guid id, CancellationToken ct)
    {
        await _portalScope.GetCurrentCustomerIdAsync(ct);
        return (await _mediator.Send(new GetOrderRevisionsQuery(id), ct)).ToOk();
    }

    [HttpPost("orders/{id:guid}/revisions")]
    public async Task<IActionResult> RequestOrderRevision(
        Guid id,
        [FromBody] PortalRequestRevisionRequest body,
        CancellationToken ct)
    {
        await _portalScope.GetCurrentCustomerIdAsync(ct);
        var cmd = new RequestOrderRevisionCommand(id, body?.ProposedLines ?? Array.Empty<RevisionLineInput>(), body?.RequestNotes);
        return (await _mediator.Send(cmd, ct)).ToCreated();
    }

    [HttpPost("orders/{id:guid}/revisions/{revisionId:guid}/approve")]
    public async Task<IActionResult> ApproveOrderRevision(Guid id, Guid revisionId, CancellationToken ct)
    {
        await _portalScope.GetCurrentCustomerIdAsync(ct);
        return (await _mediator.Send(new ApproveOrderRevisionCommand(id, revisionId), ct)).ToOk();
    }

    [HttpPost("orders/{id:guid}/revisions/{revisionId:guid}/reject")]
    public async Task<IActionResult> RejectOrderRevision(
        Guid id,
        Guid revisionId,
        [FromBody] PortalRejectRevisionRequest body,
        CancellationToken ct)
    {
        await _portalScope.GetCurrentCustomerIdAsync(ct);
        return (await _mediator.Send(new RejectOrderRevisionCommand(id, revisionId, body?.Reason ?? string.Empty), ct)).ToOk();
    }

    [HttpPost("orders/{id:guid}/revisions/{revisionId:guid}/cancel")]
    public async Task<IActionResult> CancelOrderRevision(Guid id, Guid revisionId, CancellationToken ct)
    {
        await _portalScope.GetCurrentCustomerIdAsync(ct);
        return (await _mediator.Send(new CancelOrderRevisionCommand(id, revisionId), ct)).ToOk();
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetCustomerPortalInvoicesQuery(status, page, pageSize), ct)).ToOk();

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetCustomerPortalInvoiceByIdQuery(id), ct)).ToOk();

    [HttpGet("dealers")]
    public async Task<IActionResult> GetDealers(CancellationToken ct)
        => (await _mediator.Send(new GetCustomerPortalDealersQuery(), ct)).ToOk();

    [HttpGet("approvals")]
    public async Task<IActionResult> GetPendingApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetCustomerPortalPendingApprovalsQuery(page, pageSize), ct)).ToOk();

    [HttpGet("approvals/{id:guid}")]
    public async Task<IActionResult> GetApprovalById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetCustomerPortalApprovalByIdQuery(id), ct)).ToOk();

    [HttpPost("approvals/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ApproveDealerOrderCommand(id), ct)).ToOk();

    [HttpPost("approvals/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectDealerOrderRequest body, CancellationToken ct)
        => (await _mediator.Send(new RejectDealerOrderCommand(id, body?.Reason ?? string.Empty), ct)).ToOk();

    [HttpPost("orders")]
    public async Task<IActionResult> CreateDirectOrder([FromBody] CreateCustomerDirectOrderCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToCreated();

    [HttpGet("catalog/products")]
    public async Task<IActionResult> GetCatalogProducts(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListCustomerCatalogProductsQuery(search, page, pageSize), ct)).ToOk();

    [HttpGet("dealer-links/{linkId:guid}/product-visibility")]
    public async Task<IActionResult> GetDealerProductVisibility(Guid linkId, CancellationToken ct)
        => (await _mediator.Send(new GetDealerProductVisibilityQuery(linkId), ct)).ToOk();

    [HttpPut("dealer-links/{linkId:guid}/product-visibility")]
    public async Task<IActionResult> SetDealerProductVisibility(
        Guid linkId,
        [FromBody] SetDealerProductVisibilityRequest body,
        CancellationToken ct)
    {
        var command = new SetDealerProductVisibilityCommand(
            linkId,
            body?.Mode ?? DealerProductVisibilityModes.All,
            body?.ProductIds ?? Array.Empty<Guid>());
        return (await _mediator.Send(command, ct)).ToOk();
    }

    [HttpGet("orders/{orderId:guid}/comments")]
    public async Task<IActionResult> ListOrderComments(Guid orderId, CancellationToken ct)
        => (await _mediator.Send(new ListCustomerPortalOrderCommentsQuery(orderId), ct)).ToOk();

    [HttpPost("orders/{orderId:guid}/comments")]
    public async Task<IActionResult> PostOrderComment(
        Guid orderId,
        [FromBody] PostOrderCommentRequest body,
        CancellationToken ct)
        => (await _mediator.Send(new PostCustomerPortalOrderCommentCommand(orderId, body?.Body ?? string.Empty), ct)).ToCreated();

    [HttpGet("invoices/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(Guid id, CancellationToken ct)
    {
        var customerId = await _portalScope.GetCurrentCustomerIdAsync(ct);
        var doc = await _documents.RenderInvoicePdfForCustomerAsync(id, customerId, ct);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{doc.FileName}\"";
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpGet("orders/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadOrderPdf(Guid id, CancellationToken ct)
    {
        var customerId = await _portalScope.GetCurrentCustomerIdAsync(ct);
        var doc = await _documents.RenderOrderPdfForCustomerAsync(id, customerId, ct);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{doc.FileName}\"";
        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    [HttpPost("invoices/{id:guid}/pay")]
    public async Task<IActionResult> PayInvoice(Guid id, [FromBody] PayInvoiceRequest? body, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new InitiateInvoicePaymentCommand(id, body?.BillingInfo, ip, body?.GatewayName);
        return (await _mediator.Send(command, ct)).ToOk();
    }

    [HttpGet("statement")]
    public async Task<IActionResult> GetStatement(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        var customerId = await _portalScope.GetCurrentCustomerIdAsync(ct);
        var statement = await _mediator.Send(new GetCustomerStatementQuery(customerId, from, to), ct);
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            return statement.ToOk();
        }

        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _tenants.GetByIdAsync(tenantId, ct);
        var document = CustomerStatementReportBuilder.Build(statement, tenant);
        var fmt = string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase)
            ? ReportFormat.Xlsx
            : ReportFormat.Pdf;
        var reportKey = $"customer-statement-{(statement.CustomerCode ?? customerId.ToString("N"))}";
        var file = await _reportFileFactory.RenderAsync(document, fmt, reportKey, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => (await _mediator.Send(new GetPortalProfileQuery(), ct)).ToOk();

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePortalProfileRequest body, CancellationToken ct)
    {
        var command = new UpdatePortalProfileCommand(body?.FirstName, body?.LastName, body?.PhoneNumber, body?.PreferredLocale);
        return (await _mediator.Send(command, ct)).ToOk();
    }

    [HttpGet("profile/sessions")]
    public async Task<IActionResult> ListSessions(CancellationToken ct)
        => (await _mediator.Send(new ListPortalSessionsQuery(), ct)).ToOk();

    [HttpPost("profile/sessions/revoke-all")]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
        => (await _mediator.Send(new RevokeAllPortalSessionsCommand(), ct)).ToOk();

    [HttpGet("notifications")]
    public async Task<IActionResult> ListNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => (await _mediator.Send(new ListPortalNotificationsQuery(unreadOnly, take), ct)).ToOk();

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
        => (await _mediator.Send(new GetPortalUnreadCountQuery(), ct)).ToOk();

    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken ct)
        => (await _mediator.Send(new MarkPortalNotificationReadCommand(id), ct)).ToOk();

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken ct)
        => (await _mediator.Send(new MarkAllPortalNotificationsReadCommand(), ct)).ToOk();

    [HttpGet("addresses")]
    public async Task<IActionResult> ListAddresses(CancellationToken ct)
        => (await _mediator.Send(new ListPortalAddressesQuery(), ct)).ToOk();

    [HttpPost("addresses")]
    public async Task<IActionResult> CreateAddress([FromBody] CreatePortalAddressCommand command, CancellationToken ct)
        => (await _mediator.Send(command, ct)).ToCreated();

    [HttpPut("addresses/{id:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdatePortalAddressRequest body, CancellationToken ct)
    {
        var command = new UpdatePortalAddressCommand(
            id,
            body?.Label ?? string.Empty,
            body?.Line1 ?? string.Empty,
            body?.Line2,
            body?.City,
            body?.State,
            body?.PostalCode,
            body?.Country,
            body?.IsPrimary ?? false);
        return (await _mediator.Send(command, ct)).ToOk();
    }

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeletePortalAddressCommand(id), ct)).ToOk();

    [HttpGet("credit")]
    public async Task<IActionResult> GetCreditSnapshot(CancellationToken ct)
        => (await _mediator.Send(new GetPortalCreditSnapshotQuery(), ct)).ToOk();

    [HttpGet("notification-preferences")]
    public async Task<IActionResult> ListNotificationPreferences(CancellationToken ct)
        => (await _mediator.Send(new ListPortalNotificationPreferencesQuery(), ct)).ToOk();

    [HttpPut("notification-preferences")]
    public async Task<IActionResult> UpdateNotificationPreference(
        [FromBody] UpdatePortalNotificationPreferenceRequest body,
        CancellationToken ct)
    {
        var command = new UpdatePortalNotificationPreferenceCommand(
            body?.NotificationKind ?? string.Empty,
            body?.EmailEnabled ?? true,
            body?.InAppEnabled ?? true);
        return (await _mediator.Send(command, ct)).ToOk();
    }
}

public record RejectDealerOrderRequest(string Reason);

public record PortalRequestRevisionRequest(IReadOnlyList<RevisionLineInput> ProposedLines, string? RequestNotes);

public record PortalRejectRevisionRequest(string Reason);

public record SetDealerProductVisibilityRequest(string Mode, IReadOnlyList<Guid> ProductIds);

public record PostOrderCommentRequest(string Body);

public record PayInvoiceRequest(PortalBillingInfoInput? BillingInfo, string? GatewayName);

public record UpdatePortalProfileRequest(string? FirstName, string? LastName, string? PhoneNumber, string? PreferredLocale);

public record UpdatePortalNotificationPreferenceRequest(string NotificationKind, bool EmailEnabled, bool InAppEnabled);

public record UpdatePortalAddressRequest(
    string Label,
    string Line1,
    string? Line2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    bool IsPrimary);
