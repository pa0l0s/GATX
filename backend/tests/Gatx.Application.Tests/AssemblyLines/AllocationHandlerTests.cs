using Gatx.Application.AssemblyLines.Allocations.Commands;
using Gatx.Domain.Entities;
using Gatx.Infrastructure.Persistence;
using Xunit;

namespace Gatx.Application.Tests.AssemblyLines;

public sealed class AllocationHandlerTests
{
    [Fact]
    public async Task Allocate_reorder_and_remove_keep_positions_contiguous()
    {
        await using var dbContext = TestDb.Create();
        var (lineId, w1, w2, w3) = await SeedLineWithWorkstationsAsync(dbContext);

        var allocate = new AllocateWorkstationCommandHandler(dbContext);
        await allocate.Handle(new AllocateWorkstationCommand(lineId, w1), CancellationToken.None);
        await allocate.Handle(new AllocateWorkstationCommand(lineId, w2), CancellationToken.None);
        var afterAdd = await allocate.Handle(new AllocateWorkstationCommand(lineId, w3), CancellationToken.None);

        Assert.Equal(new[] { w1, w2, w3 }, afterAdd.Select(a => a.WorkstationId));
        Assert.Equal(new[] { 1, 2, 3 }, afterAdd.Select(a => a.Position));

        // Duplicate allocation is rejected.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            allocate.Handle(new AllocateWorkstationCommand(lineId, w1), CancellationToken.None));

        var reorder = new ReorderAllocationsCommandHandler(dbContext);
        var afterReorder = await reorder.Handle(
            new ReorderAllocationsCommand(lineId, new[] { w3, w1, w2 }),
            CancellationToken.None);

        Assert.Equal(new[] { w3, w1, w2 }, afterReorder.Select(a => a.WorkstationId));
        Assert.Equal(new[] { 1, 2, 3 }, afterReorder.Select(a => a.Position));

        var remove = new RemoveAllocationCommandHandler(dbContext);
        var afterRemove = await remove.Handle(
            new RemoveAllocationCommand(lineId, w1),
            CancellationToken.None);

        Assert.Equal(new[] { w3, w2 }, afterRemove.Select(a => a.WorkstationId));
        Assert.Equal(new[] { 1, 2 }, afterRemove.Select(a => a.Position));
    }

    [Fact]
    public async Task Reorder_rejects_a_different_set()
    {
        await using var dbContext = TestDb.Create();
        var (lineId, w1, w2, _) = await SeedLineWithWorkstationsAsync(dbContext);

        var allocate = new AllocateWorkstationCommandHandler(dbContext);
        await allocate.Handle(new AllocateWorkstationCommand(lineId, w1), CancellationToken.None);
        await allocate.Handle(new AllocateWorkstationCommand(lineId, w2), CancellationToken.None);

        var reorder = new ReorderAllocationsCommandHandler(dbContext);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            reorder.Handle(new ReorderAllocationsCommand(lineId, new[] { w1 }), CancellationToken.None));
    }

    private static async Task<(Guid LineId, Guid W1, Guid W2, Guid W3)> SeedLineWithWorkstationsAsync(
        AppDbContext dbContext)
    {
        var product = new Product("8DAB");
        var line = new AssemblyLine(product.Id, "Final assembly line", true);
        var w1 = new Workstation("FRM", "Frame assembly", "PC-FRM-01");
        var w2 = new Workstation("DRV", "Drive assembly", "PC-DRV-01");
        var w3 = new Workstation("FIN", "Final inspection", "PC-FIN-01");

        dbContext.Products.Add(product);
        dbContext.AssemblyLines.Add(line);
        dbContext.Workstations.AddRange(w1, w2, w3);
        await dbContext.SaveChangesAsync();

        return (line.Id, w1.Id, w2.Id, w3.Id);
    }
}
