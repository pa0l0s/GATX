using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Products.Queries;

public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public sealed class GetProductsQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new ProductDto(
                product.Id,
                product.Name,
                product.AssemblyLines.Count))
            .ToListAsync(cancellationToken);
    }
}
