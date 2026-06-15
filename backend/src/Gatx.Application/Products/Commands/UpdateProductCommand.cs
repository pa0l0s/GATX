using FluentValidation;
using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Products.Commands;

public sealed record UpdateProductCommand(Guid Id, string Name) : IRequest<ProductDto>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
    }
}

public sealed class UpdateProductCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(item => item.AssemblyLines)
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{request.Id}' was not found.");
        }

        product.Rename(request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductDto(product.Id, product.Name, product.AssemblyLines.Count);
    }
}
