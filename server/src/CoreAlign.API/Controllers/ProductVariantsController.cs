using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Products.Variants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products/{productId:guid}/variants")]
public class ProductVariantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductVariantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListProductVariantsQuery(productId), cancellationToken);
        return result.ToOk();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid productId,
        [FromBody] CreateProductVariantRequest body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest(ApiResponse<object>.Failure("Body is required.", 400));
        }

        var command = new CreateProductVariantCommand(
            productId,
            body.Sku,
            body.Barcode,
            body.VariantAttributesJson ?? "{}",
            body.PriceOverride,
            body.StockQuantity,
            body.IsActive);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{variantId:guid}")]
    public async Task<IActionResult> Update(
        Guid productId,
        Guid variantId,
        [FromBody] UpdateProductVariantRequest body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest(ApiResponse<object>.Failure("Body is required.", 400));
        }

        var command = new UpdateProductVariantCommand(
            productId,
            variantId,
            body.Sku,
            body.Barcode,
            body.VariantAttributesJson ?? "{}",
            body.PriceOverride,
            body.IsActive);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{variantId:guid}")]
    public async Task<IActionResult> Delete(
        Guid productId,
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteProductVariantCommand(productId, variantId), cancellationToken);
        return result.ToOk();
    }
}

public sealed record CreateProductVariantRequest(
    string Sku,
    string? Barcode,
    string? VariantAttributesJson,
    decimal? PriceOverride,
    decimal StockQuantity,
    bool IsActive);

public sealed record UpdateProductVariantRequest(
    string Sku,
    string? Barcode,
    string? VariantAttributesJson,
    decimal? PriceOverride,
    bool IsActive);
