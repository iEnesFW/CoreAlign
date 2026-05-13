using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Products.Commands;
using CoreAlign.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProductsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(page, pageSize, search, isActive), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> GetStockTransactionsAsync(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetStockTransactionsQuery(id, page, pageSize), cancellationToken);
        return result.ToOk();
    }

    [HttpPost]
    public async Task<IActionResult> CreateProductAsync([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProductAsync(Guid id, [FromBody] UpdateProductCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeleteProductAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/components")]
    public async Task<IActionResult> GetComponentsAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProductComponentsQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/components")]
    public async Task<IActionResult> AddComponentAsync(Guid id, [FromBody] AddProductComponentCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ParentProductId)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}/components/{componentId:guid}")]
    public async Task<IActionResult> UpdateComponentAsync(Guid id, Guid componentId, [FromBody] UpdateProductComponentCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ParentProductId || componentId != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{id:guid}/components/{componentId:guid}")]
    public async Task<IActionResult> RemoveComponentAsync(Guid id, Guid componentId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveProductComponentCommand(id, componentId), cancellationToken);
        return result.ToOk();
    }
}
