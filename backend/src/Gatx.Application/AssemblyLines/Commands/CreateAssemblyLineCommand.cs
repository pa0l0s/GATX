using FluentValidation;
using Gatx.Application.Common.Interfaces;
using Gatx.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.AssemblyLines.Commands;

public sealed record CreateAssemblyLineCommand(Guid ProductId, string Name, bool Active)
    : IRequest<AssemblyLineDto>;

public sealed class CreateAssemblyLineCommandValidator : AbstractValidator<CreateAssemblyLineCommand>
{
    public CreateAssemblyLineCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
    }
}

public sealed class CreateAssemblyLineCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<CreateAssemblyLineCommand, AssemblyLineDto>
{
    public async Task<AssemblyLineDto> Handle(CreateAssemblyLineCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{request.ProductId}' was not found.");
        }

        var line = new AssemblyLine(product.Id, request.Name, request.Active);
        dbContext.AssemblyLines.Add(line);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AssemblyLineDto(line.Id, line.Name, line.Active, product.Id, product.Name, 0);
    }
}
