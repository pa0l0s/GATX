using Gatx.Application.AssemblyLines.Commands;
using Gatx.Application.AssemblyLines.Queries;
using Gatx.Domain.Entities;
using Xunit;

namespace Gatx.Application.Tests.AssemblyLines;

public sealed class AssemblyLineHandlerTests
{
    [Fact]
    public async Task Create_assigns_line_to_product()
    {
        await using var dbContext = TestDb.Create();
        var product = new Product("8DAB");
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var handler = new CreateAssemblyLineCommandHandler(dbContext);
        var line = await handler.Handle(
            new CreateAssemblyLineCommand(product.Id, " Convey line ", true),
            CancellationToken.None);

        Assert.Equal("Convey line", line.Name);
        Assert.Equal(product.Id, line.ProductId);
        Assert.Equal("8DAB", line.ProductName);
        Assert.True(line.Active);
    }

    [Fact]
    public async Task Create_rejects_missing_product()
    {
        await using var dbContext = TestDb.Create();
        var handler = new CreateAssemblyLineCommandHandler(dbContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new CreateAssemblyLineCommand(Guid.NewGuid(), "Convey line", true), CancellationToken.None));
    }

    [Fact]
    public async Task Get_filters_by_product()
    {
        await using var dbContext = TestDb.Create();
        var productA = new Product("8DAB");
        var productB = new Product("Simosec");
        dbContext.Products.AddRange(productA, productB);
        dbContext.AssemblyLines.AddRange(
            new AssemblyLine(productA.Id, "Convey line", true),
            new AssemblyLine(productA.Id, "Manual line", true),
            new AssemblyLine(productB.Id, "Final assembly line", true));
        await dbContext.SaveChangesAsync();

        var handler = new GetAssemblyLinesQueryHandler(dbContext);
        var forA = await handler.Handle(new GetAssemblyLinesQuery(productA.Id), CancellationToken.None);
        var all = await handler.Handle(new GetAssemblyLinesQuery(null), CancellationToken.None);

        Assert.Equal(2, forA.Count);
        Assert.All(forA, line => Assert.Equal(productA.Id, line.ProductId));
        Assert.Equal(3, all.Count);
    }
}
