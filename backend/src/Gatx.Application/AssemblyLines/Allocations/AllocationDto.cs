namespace Gatx.Application.AssemblyLines.Allocations;

public sealed record AllocationDto(
    Guid WorkstationId,
    string ShortName,
    string Name,
    string PcName,
    int Position);
