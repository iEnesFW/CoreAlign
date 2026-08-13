using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Vendors.Commands;
using CoreAlign.Application.Vendors.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;
    public VendorsController(IMediator mediator) => _mediator = mediator;

    // ---------- Vendor master ----------

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] VendorStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new SearchVendorsQuery(search, status, page, pageSize), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetVendorByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateVendorCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var approverId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        return (await _mediator.Send(new ApproveVendorCommand(id, approverId), ct)).ToOk();
    }

    [HttpPost("{id:guid}/block")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockVendorCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ArchiveVendorCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/rating")]
    public async Task<IActionResult> SetRating(Guid id, [FromBody] SetVendorRatingCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteVendorCommand(id), ct)).ToOk();

    // ---------- Vendor children ----------

    [HttpGet("{id:guid}/addresses")]
    public async Task<IActionResult> Addresses(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetVendorAddressesQuery(id), ct)).ToOk();

    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> CreateAddress(Guid id, [FromBody] CreateVendorAddressCommand cmd, CancellationToken ct)
    {
        if (id != cmd.VendorId) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToCreated();
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] UpdateVendorAddressCommand cmd, CancellationToken ct)
    {
        if (addressId != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken ct)
        => (await _mediator.Send(new DeleteVendorAddressCommand(addressId), ct)).ToOk();

    [HttpGet("{id:guid}/contacts")]
    public async Task<IActionResult> Contacts(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetVendorContactsQuery(id), ct)).ToOk();

    [HttpPost("{id:guid}/contacts")]
    public async Task<IActionResult> CreateContact(Guid id, [FromBody] CreateVendorContactCommand cmd, CancellationToken ct)
    {
        if (id != cmd.VendorId) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToCreated();
    }

    [HttpPut("contacts/{contactId:guid}")]
    public async Task<IActionResult> UpdateContact(Guid contactId, [FromBody] UpdateVendorContactCommand cmd, CancellationToken ct)
    {
        if (contactId != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpDelete("contacts/{contactId:guid}")]
    public async Task<IActionResult> DeleteContact(Guid contactId, CancellationToken ct)
        => (await _mediator.Send(new DeleteVendorContactCommand(contactId), ct)).ToOk();

    [HttpGet("{id:guid}/bank-accounts")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> BankAccounts(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetVendorBankAccountsQuery(id), ct)).ToOk();

    [HttpPost("{id:guid}/bank-accounts")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> CreateBankAccount(Guid id, [FromBody] CreateVendorBankAccountCommand cmd, CancellationToken ct)
    {
        if (id != cmd.VendorId) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToCreated();
    }

    [HttpPut("bank-accounts/{accountId:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateBankAccount(Guid accountId, [FromBody] UpdateVendorBankAccountCommand cmd, CancellationToken ct)
    {
        if (accountId != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpDelete("bank-accounts/{accountId:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeleteBankAccount(Guid accountId, CancellationToken ct)
        => (await _mediator.Send(new DeleteVendorBankAccountCommand(accountId), ct)).ToOk();

    [HttpGet("{id:guid}/ledger")]
    public async Task<IActionResult> Ledger(
        Guid id,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetVendorLedgerQuery(id, fromUtc, toUtc, page, pageSize), ct)).ToOk();
}
