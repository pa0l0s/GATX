using FluentValidation;
using Gatx.Application.Common.Interfaces;
using Gatx.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Products.Commands;

public sealed record CreateProductCommand(string Name) : IRequest<ProductDto>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(120);
    }
}

public sealed class CreateProductCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var exists = await dbContext.Products
            .AnyAsync(product => product.Name == normalizedName, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Product '{normalizedName}' already exists.");
        }

        var product = new Product(normalizedName);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductDto(product.Id, product.Name, 0);
    }
}
