using Gatx.Application.Common.Interfaces;
using MediatR;

namespace Gatx.Application.AssemblyLines.Allocations.Queries;

public sealed record GetAllocationsQuery(Guid AssemblyLineId) : IRequest<IReadOnlyList<AllocationDto>>;

public sealed class GetAllocationsQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetAllocationsQuery, IReadOnlyList<AllocationDto>>
{
    public async Task<IReadOnlyList<AllocationDto>> Handle(
        GetAllocationsQuery request,
        CancellationToken cancellationToken)
    {
        await AllocationOrdering.EnsureLineExistsAsync(dbContext, request.AssemblyLineId, cancellationToken);
        return await AllocationOrdering.LoadAsync(dbContext, request.AssemblyLineId, cancellationToken);
    }
}
