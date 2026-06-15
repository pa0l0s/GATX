using Gatx.Application.Products.Commands;
using Gatx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gatx.Application.Tests.Products;

public sealed class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_creates_product_with_trimmed_name()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreateProductCommandHandler(dbContext);

        var product = await handler.Handle(new CreateProductCommand(" 8DAB "), CancellationToken.None);

        Assert.Equal("8DAB", product.Name);
        Assert.True(await dbContext.Products.AnyAsync(item => item.Name == "8DAB"));
    }

    [Fact]
    public async Task Handle_rejects_duplicate_product_name()
    {
        await using var dbContext = CreateDbContext();
        var handler = new CreateProductCommandHandler(dbContext);
        await handler.Handle(new CreateProductCommand("8DAB"), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateProductCommand("8DAB"), CancellationToken.None));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
