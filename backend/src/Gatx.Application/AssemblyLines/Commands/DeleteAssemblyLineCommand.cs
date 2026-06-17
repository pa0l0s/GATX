using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Commands;

public sealed record DeleteAssemblyLineCommand(Guid Id) : IRequest;

public sealed class DeleteAssemblyLineCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<DeleteAssemblyLineCommand>
{
    public async Task Handle(DeleteAssemblyLineCommand request, CancellationToken cancellationToken)
    {
        var line = await dbContext.AssemblyLines
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (line is null)
        {
            throw new KeyNotFoundException($"Assembly line '{request.Id}' was not found.");
        }

        dbContext.AssemblyLines.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
