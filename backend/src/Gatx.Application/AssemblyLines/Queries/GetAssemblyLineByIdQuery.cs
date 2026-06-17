using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Queries;

public sealed record GetAssemblyLineByIdQuery(Guid Id) : IRequest<AssemblyLineDto>;

public sealed class GetAssemblyLineByIdQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetAssemblyLineByIdQuery, AssemblyLineDto>
{
    public async Task<AssemblyLineDto> Handle(GetAssemblyLineByIdQuery request, CancellationToken cancellationToken)
    {
        var line = await dbContext.AssemblyLines
            .AsNoTracking()
            .Where(item => item.Id == request.Id)
            .Select(item => new AssemblyLineDto(
                item.Id,
                item.Name,
                item.Active,
                item.ProductId,
                item.Product!.Name,
                item.Workstations.Count))
            .SingleOrDefaultAsync(cancellationToken);

        if (line is null)
        {
            throw new KeyNotFoundException($"Assembly line '{request.Id}' was not found.");
        }

        return line;
    }
}
