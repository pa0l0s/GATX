namespace Gatx.Application.AssemblyLines;

public sealed record AssemblyLineDto(
    Guid Id,
    string Name,
    bool Active,
    Guid ProductId,
    string ProductName,
    int WorkstationCount);
