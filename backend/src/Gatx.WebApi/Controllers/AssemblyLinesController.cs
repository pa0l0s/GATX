using Gatx.Application.AssemblyLines;
using Gatx.Application.AssemblyLines.Allocations;
using Gatx.Application.AssemblyLines.Allocations.Commands;
using Gatx.Application.AssemblyLines.Allocations.Queries;
using Gatx.Application.AssemblyLines.Commands;
using Gatx.Application.AssemblyLines.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gatx.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/assembly-lines")]
public sealed class AssemblyLinesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AssemblyLineDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AssemblyLineDto>> Get(
        [FromQuery] Guid? productId,
        CancellationToken cancellationToken)
    {
        return await sender.Send(new GetAssemblyLinesQuery(productId), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AssemblyLineDto>(StatusCodes.Status200OK)]
    public async Task<AssemblyLineDto> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetAssemblyLineByIdQuery(id), cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType<AssemblyLineDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AssemblyLineDto>> Create(
        [FromBody] CreateAssemblyLineRequest request,
        CancellationToken cancellationToken)
    {
        var line = await sender.Send(
            new CreateAssemblyLineCommand(request.ProductId, request.Name, request.Active),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = line.Id }, line);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AssemblyLineDto>(StatusCodes.Status200OK)]
    public async Task<AssemblyLineDto> Update(
        Guid id,
        [FromBody] UpdateAssemblyLineRequest request,
        CancellationToken cancellationToken)
    {
        return await sender.Send(
            new UpdateAssemblyLineCommand(id, request.ProductId, request.Name, request.Active),
            cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAssemblyLineCommand(id), cancellationToken);
        return NoContent();
    }

    // ── Allocations (ordered workstations on a line) ──────────────────────────

    [HttpGet("{id:guid}/workstations")]
    [ProducesResponseType<IReadOnlyList<AllocationDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AllocationDto>> GetAllocations(Guid id, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetAllocationsQuery(id), cancellationToken);
    }

    [HttpPost("{id:guid}/workstations")]
    [ProducesResponseType<IReadOnlyList<AllocationDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<IReadOnlyList<AllocationDto>>> Allocate(
        Guid id,
        [FromBody] AllocateWorkstationRequest request,
        CancellationToken cancellationToken)
    {
        var allocations = await sender.Send(
            new AllocateWorkstationCommand(id, request.WorkstationId),
            cancellationToken);
        return CreatedAtAction(nameof(GetAllocations), new { id }, allocations);
    }

    [HttpPut("{id:guid}/workstations/order")]
    [ProducesResponseType<IReadOnlyList<AllocationDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AllocationDto>> Reorder(
        Guid id,
        [FromBody] ReorderAllocationsRequest request,
        CancellationToken cancellationToken)
    {
        return await sender.Send(
            new ReorderAllocationsCommand(id, request.WorkstationIds),
            cancellationToken);
    }

    [HttpDelete("{id:guid}/workstations/{workstationId:guid}")]
    [ProducesResponseType<IReadOnlyList<AllocationDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AllocationDto>> RemoveAllocation(
        Guid id,
        Guid workstationId,
        CancellationToken cancellationToken)
    {
        return await sender.Send(new RemoveAllocationCommand(id, workstationId), cancellationToken);
    }
}

public sealed record CreateAssemblyLineRequest(Guid ProductId, string Name, bool Active);
public sealed record UpdateAssemblyLineRequest(Guid ProductId, string Name, bool Active);
public sealed record AllocateWorkstationRequest(Guid WorkstationId);
public sealed record ReorderAllocationsRequest(IReadOnlyList<Guid> WorkstationIds);
