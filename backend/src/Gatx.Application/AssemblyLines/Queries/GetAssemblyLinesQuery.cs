using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Queries;

public sealed record GetAssemblyLinesQuery(Guid? ProductId) : IRequest<IReadOnlyList<AssemblyLineDto>>;

public sealed class GetAssemblyLinesQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetAssemblyLinesQuery, IReadOnlyList<AssemblyLineDto>>
{
    public async Task<IReadOnlyList<AssemblyLineDto>> Handle(
        GetAssemblyLinesQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AssemblyLines.AsNoTracking();

        if (request.ProductId is { } productId)
        {
            query = query.Where(line => line.ProductId == productId);
        }

        return await query
            .OrderBy(line => line.Name)
            .Select(line => new AssemblyLineDto(
                line.Id,
                line.Name,
                line.Active,
                line.ProductId,
                line.Product!.Name,
                line.Workstations.Count))
            .ToListAsync(cancellationToken);
    }
}
