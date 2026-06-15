using Gatx.Application.Products;
using Gatx.Application.Products.Commands;
using Gatx.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gatx.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ProductDto>> Get(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetProductsQuery(), cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await sender.Send(new CreateProductCommand(request.Name), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    public async Task<ProductDto> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        return await sender.Send(new UpdateProductCommand(id, request.Name), cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateProductRequest(string Name);
public sealed record UpdateProductRequest(string Name);
