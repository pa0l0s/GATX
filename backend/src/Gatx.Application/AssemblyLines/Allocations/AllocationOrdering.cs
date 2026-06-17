using Gatx.Application.Common.Interfaces;
using Gatx.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Allocations;

internal static class AllocationOrdering
{
    private const int TemporaryOffset = 100_000;

    /// <summary>
    /// Reassigns contiguous 1-based positions to the given ordered allocations.
    /// Writes through a temporary offset first so the unique (line, position) index
    /// is never violated by a transient duplicate during the reshuffle.
    /// </summary>
    public static async Task ReassignPositionsAsync(
        IAppDbContext dbContext,
        IReadOnlyList<AssemblyLineWorkstation> ordered,
        CancellationToken cancellationToken)
    {
        if (ordered.Count == 0)
        {
            return;
        }

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].MoveTo(TemporaryOffset + index + 1);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].MoveTo(index + 1);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static async Task<IReadOnlyList<AllocationDto>> LoadAsync(
        IAppDbContext dbContext,
        Guid assemblyLineId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AssemblyLineWorkstations
            .AsNoTracking()
            .Where(allocation => allocation.AssemblyLineId == assemblyLineId)
            .OrderBy(allocation => allocation.Position)
            .Select(allocation => new AllocationDto(
                allocation.WorkstationId,
                allocation.Workstation!.ShortName,
                allocation.Workstation.Name,
                allocation.Workstation.PcName,
                allocation.Position))
            .ToListAsync(cancellationToken);
    }

    public static async Task EnsureLineExistsAsync(
        IAppDbContext dbContext,
        Guid assemblyLineId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.AssemblyLines
            .AnyAsync(line => line.Id == assemblyLineId, cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException($"Assembly line '{assemblyLineId}' was not found.");
        }
    }
}
