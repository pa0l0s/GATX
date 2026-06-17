using FluentValidation;
using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Workstations.Commands;

public sealed record UpdateWorkstationCommand(Guid Id, string ShortName, string Name, string PcName)
    : IRequest<WorkstationDto>;

public sealed class UpdateWorkstationCommandValidator : AbstractValidator<UpdateWorkstationCommand>
{
    public UpdateWorkstationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.ShortName).NotEmpty().MaximumLength(40);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.PcName).NotEmpty().MaximumLength(80);
    }
}

public sealed class UpdateWorkstationCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<UpdateWorkstationCommand, WorkstationDto>
{
    public async Task<WorkstationDto> Handle(UpdateWorkstationCommand request, CancellationToken cancellationToken)
    {
        var workstation = await dbContext.Workstations
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (workstation is null)
        {
            throw new KeyNotFoundException($"Workstation '{request.Id}' was not found.");
        }

        workstation.Update(request.ShortName, request.Name, request.PcName);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WorkstationDto(workstation.Id, workstation.ShortName, workstation.Name, workstation.PcName);
    }
}
