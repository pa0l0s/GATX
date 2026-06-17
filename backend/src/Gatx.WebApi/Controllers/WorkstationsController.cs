using Gatx.Application.Workstations;
using Gatx.Application.Workstations.Commands;
using Gatx.Application.Workstations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gatx.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class WorkstationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WorkstationDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<WorkstationDto>> Get(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetWorkstationsQuery(), cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType<WorkstationDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkstationDto>> Create(
        [FromBody] CreateWorkstationRequest request,
        CancellationToken cancellationToken)
    {
        var workstation = await sender.Send(
            new CreateWorkstationCommand(request.ShortName, request.Name, request.PcName),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = workstation.Id }, workstation);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<WorkstationDto>(StatusCodes.Status200OK)]
    public async Task<WorkstationDto> Update(
        Guid id,
        [FromBody] UpdateWorkstationRequest request,
        CancellationToken cancellationToken)
    {
        return await sender.Send(
            new UpdateWorkstationCommand(id, request.ShortName, request.Name, request.PcName),
            cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteWorkstationCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateWorkstationRequest(string ShortName, string Name, string PcName);
public sealed record UpdateWorkstationRequest(string ShortName, string Name, string PcName);
