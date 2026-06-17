using FluentValidation;
using Gatx.Application.Common.Interfaces;
using Gatx.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Allocations.Commands;

public sealed record AllocateWorkstationCommand(Guid AssemblyLineId, Guid WorkstationId)
    : IRequest<IReadOnlyList<AllocationDto>>;

public sealed class AllocateWorkstationCommandValidator : AbstractValidator<AllocateWorkstationCommand>
{
    public AllocateWorkstationCommandValidator()
    {
        RuleFor(command => command.AssemblyLineId).NotEmpty();
        RuleFor(command => command.WorkstationId).NotEmpty();
    }
}

public sealed class AllocateWorkstationCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<AllocateWorkstationCommand, IReadOnlyList<AllocationDto>>
{
    public async Task<IReadOnlyList<AllocationDto>> Handle(
        AllocateWorkstationCommand request,
        CancellationToken cancellationToken)
    {
        await AllocationOrdering.EnsureLineExistsAsync(dbContext, request.AssemblyLineId, cancellationToken);

        var workstationExists = await dbContext.Workstations
            .AnyAsync(workstation => workstation.Id == request.WorkstationId, cancellationToken);

        if (!workstationExists)
        {
            throw new KeyNotFoundException($"Workstation '{request.WorkstationId}' was not found.");
        }

        var alreadyAllocated = await dbContext.AssemblyLineWorkstations
            .AnyAsync(
                allocation => allocation.AssemblyLineId == request.AssemblyLineId
                    && allocation.WorkstationId == request.WorkstationId,
                cancellationToken);

        if (alreadyAllocated)
        {
            throw new InvalidOperationException("This workstation is already allocated to the assembly line.");
        }

        var maxPosition = await dbContext.AssemblyLineWorkstations
            .Where(allocation => allocation.AssemblyLineId == request.AssemblyLineId)
            .Select(allocation => (int?)allocation.Position)
            .MaxAsync(cancellationToken) ?? 0;

        dbContext.AssemblyLineWorkstations.Add(
            new AssemblyLineWorkstation(request.AssemblyLineId, request.WorkstationId, maxPosition + 1));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await AllocationOrdering.LoadAsync(dbContext, request.AssemblyLineId, cancellationToken);
    }
}
