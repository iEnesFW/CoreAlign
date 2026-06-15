using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Pricing.PriceListItems.Commands;
using CoreAlign.Application.Pricing.PriceListItems.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/price-lists/{listId:guid}/items")]
public class PriceListItemsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PriceListItemsController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> List(Guid listId, CancellationToken ct)
        => (await _mediator.Send(new ListPriceListItemsQuery(listId), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid listId, Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetPriceListItemByIdQuery(listId, id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Add(Guid listId, [FromBody] AddPriceListItemCommand cmd, CancellationToken ct)
        => listId != cmd.PriceListId
            ? RouteMismatch()
            : (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid listId, Guid id, [FromBody] UpdatePriceListItemCommand cmd, CancellationToken ct)
        => listId != cmd.PriceListId || id != cmd.Id
            ? RouteMismatch()
            : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Remove(Guid listId, Guid id, CancellationToken ct)
        => (await _mediator.Send(new RemovePriceListItemCommand(listId, id), ct)).ToOk();
}
