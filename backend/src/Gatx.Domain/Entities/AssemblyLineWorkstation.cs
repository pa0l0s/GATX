namespace Gatx.Domain.Entities;

public sealed class AssemblyLineWorkstation
{
    private AssemblyLineWorkstation()
    {
    }

    public AssemblyLineWorkstation(Guid assemblyLineId, Guid workstationId, int position)
    {
        if (position < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position starts from 1.");
        }

        AssemblyLineId = assemblyLineId;
        WorkstationId = workstationId;
        Position = position;
    }

    public Guid AssemblyLineId { get; private set; }
    public AssemblyLine? AssemblyLine { get; private set; }
    public Guid WorkstationId { get; private set; }
    public Workstation? Workstation { get; private set; }
    public int Position { get; private set; }

    public void MoveTo(int position)
    {
        if (position < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position starts from 1.");
        }

        Position = position;
    }
}
