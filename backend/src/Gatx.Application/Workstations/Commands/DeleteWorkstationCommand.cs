using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Workstations.Commands;

public sealed record DeleteWorkstationCommand(Guid Id) : IRequest;

public sealed class DeleteWorkstationCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<DeleteWorkstationCommand>
{
    public async Task Handle(DeleteWorkstationCommand request, CancellationToken cancellationToken)
    {
        var workstation = await dbContext.Workstations
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (workstation is null)
        {
            throw new KeyNotFoundException($"Workstation '{request.Id}' was not found.");
        }

        dbContext.Workstations.Remove(workstation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
