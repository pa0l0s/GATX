using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Workstations.Queries;

public sealed record GetWorkstationsQuery : IRequest<IReadOnlyList<WorkstationDto>>;

public sealed class GetWorkstationsQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<GetWorkstationsQuery, IReadOnlyList<WorkstationDto>>
{
    public async Task<IReadOnlyList<WorkstationDto>> Handle(
        GetWorkstationsQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Workstations
            .AsNoTracking()
            .OrderBy(workstation => workstation.Name)
            .Select(workstation => new WorkstationDto(
                workstation.Id,
                workstation.ShortName,
                workstation.Name,
                workstation.PcName))
            .ToListAsync(cancellationToken);
    }
}
