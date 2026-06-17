using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Allocations.Commands;

public sealed record RemoveAllocationCommand(Guid AssemblyLineId, Guid WorkstationId)
    : IRequest<IReadOnlyList<AllocationDto>>;

public sealed class RemoveAllocationCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<RemoveAllocationCommand, IReadOnlyList<AllocationDto>>
{
    public async Task<IReadOnlyList<AllocationDto>> Handle(
        RemoveAllocationCommand request,
        CancellationToken cancellationToken)
    {
        await AllocationOrdering.EnsureLineExistsAsync(dbContext, request.AssemblyLineId, cancellationToken);

        var allocation = await dbContext.AssemblyLineWorkstations
            .SingleOrDefaultAsync(
                item => item.AssemblyLineId == request.AssemblyLineId
                    && item.WorkstationId == request.WorkstationId,
                cancellationToken);

        if (allocation is null)
        {
            throw new KeyNotFoundException("Allocation was not found.");
        }

        dbContext.AssemblyLineWorkstations.Remove(allocation);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Close the gap so positions stay contiguous (1..n).
        var remaining = await dbContext.AssemblyLineWorkstations
            .Where(item => item.AssemblyLineId == request.AssemblyLineId)
            .OrderBy(item => item.Position)
            .ToListAsync(cancellationToken);

        await AllocationOrdering.ReassignPositionsAsync(dbContext, remaining, cancellationToken);

        return await AllocationOrdering.LoadAsync(dbContext, request.AssemblyLineId, cancellationToken);
    }
}
