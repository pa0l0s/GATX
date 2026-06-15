using Gatx.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Products.Commands;

public sealed record DeleteProductCommand(Guid Id) : IRequest;

public sealed class DeleteProductCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{request.Id}' was not found.");
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
