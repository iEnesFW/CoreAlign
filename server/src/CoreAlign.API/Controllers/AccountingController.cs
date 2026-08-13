using System.Security.Claims;
using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/accounting")]
public class AccountingController : ControllerBase
{
    private readonly IMediator _mediator;
    public AccountingController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;

    [HttpGet("periods")]
    public async Task<IActionResult> ListPeriods([FromQuery] int? year, CancellationToken ct)
        => (await _mediator.Send(new ListAccountingPeriodsQuery(year), ct)).ToOk();

    [HttpGet("periods/{id:guid}")]
    public async Task<IActionResult> GetPeriod(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetAccountingPeriodByIdQuery(id), ct)).ToOk();

    [HttpPost("periods")]
    public async Task<IActionResult> CreatePeriod([FromBody] CreateAccountingPeriodCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("periods/{id:guid}/close")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ClosePeriod(Guid id, [FromBody] ClosePeriodCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new ClosePeriodCommand(id, CurrentUserId, cmd?.Notes), ct)).ToOk();

    [HttpPost("periods/{id:guid}/reopen")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ReopenPeriod(Guid id, [FromBody] ReopenPeriodCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new ReopenPeriodCommand(id, CurrentUserId), ct)).ToOk();

    [HttpPost("periods/{id:guid}/lock")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> LockPeriod(Guid id, [FromBody] LockPeriodCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new LockPeriodCommand(id, CurrentUserId), ct)).ToOk();

    // ---------- Chart of Accounts (Hesap Planı) ----------

    [HttpGet("gl-accounts")]
    public async Task<IActionResult> ListGLAccounts(
        [FromQuery] Domain.Enums.AccountType? type,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isPostable,
        [FromQuery] Guid? parentId,
        CancellationToken ct)
        => (await _mediator.Send(new ListGLAccountsQuery(type, isActive, isPostable, parentId), ct)).ToOk();

    [HttpGet("gl-accounts/tree")]
    public async Task<IActionResult> GetGLAccountTree(CancellationToken ct)
        => (await _mediator.Send(new GetGLAccountTreeQuery(), ct)).ToOk();

    [HttpGet("gl-accounts/{id:guid}")]
    public async Task<IActionResult> GetGLAccountById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetGLAccountByIdQuery(id), ct)).ToOk();

    [HttpPost("gl-accounts")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> CreateGLAccount([FromBody] CreateGLAccountCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("gl-accounts/{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateGLAccount(Guid id, [FromBody] UpdateGLAccountCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(Application.Common.ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPost("gl-accounts/{id:guid}/active")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> SetGLAccountActive(Guid id, [FromBody] SetGLAccountActiveCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(Application.Common.ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpDelete("gl-accounts/{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeleteGLAccount(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteGLAccountCommand(id), ct)).ToOk();

    [HttpPost("gl-accounts/seed-turkish")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> SeedTurkishChartOfAccounts(CancellationToken ct)
        => (await _mediator.Send(new SeedTurkishChartOfAccountsCommand(), ct)).ToOk();

    // ---------- Journal Entries (Yevmiye Fişleri) ----------

    [HttpGet("journal-entries")]
    public async Task<IActionResult> SearchJournalEntries(
        [FromQuery] string? search,
        [FromQuery] Domain.Enums.JournalEntryType? type,
        [FromQuery] Domain.Enums.JournalEntryStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(
            new SearchJournalEntriesQuery(search, type, status, fromDate, toDate, page, pageSize),
            ct)).ToOk();

    [HttpGet("journal-entries/{id:guid}")]
    public async Task<IActionResult> GetJournalEntryById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetJournalEntryByIdQuery(id), ct)).ToOk();

    [HttpPost("journal-entries")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalEntryCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("journal-entries/{id:guid}/header")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateJournalEntryHeader(Guid id, [FromBody] UpdateJournalEntryHeaderCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(Application.Common.ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPut("journal-entries/{id:guid}/lines")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ReplaceJournalEntryLines(Guid id, [FromBody] ReplaceJournalEntryLinesCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(Application.Common.ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpPost("journal-entries/{id:guid}/post")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> PostJournalEntry(Guid id, [FromBody] PostJournalEntryCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new PostJournalEntryCommand(id, CurrentUserId), ct)).ToOk();

    [HttpPost("journal-entries/{id:guid}/reverse")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ReverseJournalEntry(Guid id, [FromBody] ReverseJournalEntryCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(
            new ReverseJournalEntryCommand(id, cmd?.ReversalPostingDate, CurrentUserId), ct)).ToOk();

    [HttpDelete("journal-entries/{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeleteJournalEntry(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteJournalEntryCommand(id), ct)).ToOk();

    // ---------- Year-End Close / Opening (Kapanış / Açılış) ----------

    [HttpPost("fiscal-years/{year:int}/close")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> CloseFiscalYear(int year, [FromBody] CloseFiscalYearCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new CloseFiscalYearCommand(year, CurrentUserId), ct)).ToOk();

    [HttpPost("fiscal-years/{year:int}/open-next")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> OpenNextFiscalYear(int year, [FromBody] OpenFiscalYearCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new OpenFiscalYearCommand(year, CurrentUserId), ct)).ToOk();

    [HttpPost("fiscal-years/{year:int}/reverse-close")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ReverseFiscalYearClose(int year, [FromBody] ReverseFiscalYearCloseCommand? cmd, CancellationToken ct)
        => (await _mediator.Send(new ReverseFiscalYearCloseCommand(year, CurrentUserId), ct)).ToOk();

    // ---------- Mizan / Trial Balance ----------

    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetTrialBalance(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
        => (await _mediator.Send(new GetTrialBalanceQuery(fromDate, toDate), ct)).ToOk();

    // ---------- Financial Statements (Bilanço / Gelir Tablosu / Mutabakat) ----------

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTime asOf, CancellationToken ct)
        => (await _mediator.Send(new GetBalanceSheetQuery(asOf), ct)).ToOk();

    [HttpGet("income-statement")]
    public async Task<IActionResult> GetIncomeStatement(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken ct)
        => (await _mediator.Send(new GetIncomeStatementQuery(fromDate, toDate), ct)).ToOk();

    [HttpGet("reconciliation")]
    public async Task<IActionResult> GetReconciliation([FromQuery] DateTime asOf, CancellationToken ct)
        => (await _mediator.Send(new GetSubledgerReconciliationQuery(asOf), ct)).ToOk();
}

[ApiController]
[Authorize(Policy = PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pricing")]
public class PricingController : ControllerBase
{
    private readonly IMediator _mediator;
    public PricingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve(
        [FromQuery] Guid productId,
        [FromQuery] Guid customerId,
        [FromQuery] decimal quantity = 1m,
        [FromQuery] string? currency = null,
        CancellationToken ct = default)
        => (await _mediator.Send(new ResolvePriceQuery(productId, customerId, quantity, currency), ct)).ToOk();

    [HttpGet("customer-product-prices")]
    public async Task<IActionResult> GetCustomerProductPrices(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? productId,
        CancellationToken ct)
        => (await _mediator.Send(new GetCustomerProductPricesQuery(customerId, productId), ct)).ToOk();

    [HttpPost("customer-product-prices")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerProductPriceCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("customer-product-prices/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerProductPriceCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(Application.Common.ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpDelete("customer-product-prices/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteCustomerProductPriceCommand(id), ct)).ToOk();
}
