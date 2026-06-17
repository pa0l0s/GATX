using FluentValidation;
using Gatx.Application.Common.Interfaces;
using Gatx.Domain.Entities;
using MediatR;

namespace Gatx.Application.Workstations.Commands;

public sealed record CreateWorkstationCommand(string ShortName, string Name, string PcName)
    : IRequest<WorkstationDto>;

public sealed class CreateWorkstationCommandValidator : AbstractValidator<CreateWorkstationCommand>
{
    public CreateWorkstationCommandValidator()
    {
        RuleFor(command => command.ShortName).NotEmpty().MaximumLength(40);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.PcName).NotEmpty().MaximumLength(80);
    }
}

public sealed class CreateWorkstationCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<CreateWorkstationCommand, WorkstationDto>
{
    public async Task<WorkstationDto> Handle(CreateWorkstationCommand request, CancellationToken cancellationToken)
    {
        var workstation = new Workstation(request.ShortName, request.Name, request.PcName);
        dbContext.Workstations.Add(workstation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WorkstationDto(workstation.Id, workstation.ShortName, workstation.Name, workstation.PcName);
    }
}
