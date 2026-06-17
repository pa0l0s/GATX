using FluentValidation;
using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Allocations.Commands;

public sealed record ReorderAllocationsCommand(Guid AssemblyLineId, IReadOnlyList<Guid> WorkstationIds)
    : IRequest<IReadOnlyList<AllocationDto>>;

public sealed class ReorderAllocationsCommandValidator : AbstractValidator<ReorderAllocationsCommand>
{
    public ReorderAllocationsCommandValidator()
    {
        RuleFor(command => command.AssemblyLineId).NotEmpty();
        RuleFor(command => command.WorkstationIds).NotNull();
    }
}

public sealed class ReorderAllocationsCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<ReorderAllocationsCommand, IReadOnlyList<AllocationDto>>
{
    public async Task<IReadOnlyList<AllocationDto>> Handle(
        ReorderAllocationsCommand request,
        CancellationToken cancellationToken)
    {
        await AllocationOrdering.EnsureLineExistsAsync(dbContext, request.AssemblyLineId, cancellationToken);

        var current = await dbContext.AssemblyLineWorkstations
            .Where(allocation => allocation.AssemblyLineId == request.AssemblyLineId)
            .ToListAsync(cancellationToken);

        var requested = request.WorkstationIds;
        var currentIds = current.Select(allocation => allocation.WorkstationId).ToHashSet();

        var sameSet = requested.Count == currentIds.Count
            && requested.Distinct().Count() == requested.Count
            && requested.All(currentIds.Contains);

        if (!sameSet)
        {
            throw new ArgumentException(
                "The new order must contain exactly the workstations currently allocated to this line.");
        }

        var byWorkstation = current.ToDictionary(allocation => allocation.WorkstationId);
        var ordered = requested.Select(id => byWorkstation[id]).ToList();

        await AllocationOrdering.ReassignPositionsAsync(dbContext, ordered, cancellationToken);

        return await AllocationOrdering.LoadAsync(dbContext, request.AssemblyLineId, cancellationToken);
    }
}
