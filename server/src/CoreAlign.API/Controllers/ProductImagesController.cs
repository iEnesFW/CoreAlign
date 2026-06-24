using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Products.Images;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products/{productId:guid}/images")]
public class ProductImagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductImagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListProductImagesQuery(productId), cancellationToken);
        return result.ToOk();
    }

    [HttpPost]
    [RequestSizeLimit(ProductImagePolicy.MaxBytesPerImage + (256 * 1024))]
    [Microsoft.AspNetCore.Mvc.ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Upload(
        Guid productId,
        [FromForm] IFormFile file,
        [FromForm] string? altText,
        [FromForm] bool makePrimary,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Failure("A non-empty file is required.", StatusCodes.Status400BadRequest));
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadProductImageCommand(
            productId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            altText,
            makePrimary);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{imageId:guid}")]
    public async Task<IActionResult> Update(
        Guid productId,
        Guid imageId,
        [FromBody] UpdateProductImageRequest body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductImageCommand(
            productId,
            imageId,
            body?.AltText,
            body?.DisplayOrder ?? 0,
            body?.IsPrimary ?? false);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteProductImageCommand(productId, imageId);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }
}

public sealed record UpdateProductImageRequest(string? AltText, int DisplayOrder, bool IsPrimary);
