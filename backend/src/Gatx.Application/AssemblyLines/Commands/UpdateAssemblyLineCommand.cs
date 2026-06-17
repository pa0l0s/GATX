using FluentValidation;
using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Commands;

public sealed record UpdateAssemblyLineCommand(Guid Id, Guid ProductId, string Name, bool Active)
    : IRequest<AssemblyLineDto>;

public sealed class UpdateAssemblyLineCommandValidator : AbstractValidator<UpdateAssemblyLineCommand>
{
    public UpdateAssemblyLineCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
    }
}

public sealed class UpdateAssemblyLineCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<UpdateAssemblyLineCommand, AssemblyLineDto>
{
    public async Task<AssemblyLineDto> Handle(UpdateAssemblyLineCommand request, CancellationToken cancellationToken)
    {
        var line = await dbContext.AssemblyLines
            .Include(item => item.Workstations)
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (line is null)
        {
            throw new KeyNotFoundException($"Assembly line '{request.Id}' was not found.");
        }

        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{request.ProductId}' was not found.");
        }

        line.Rename(request.Name);
        line.SetActive(request.Active);
        line.MoveToProduct(product.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AssemblyLineDto(
            line.Id,
            line.Name,
            line.Active,
            product.Id,
            product.Name,
            line.Workstations.Count);
    }
}
