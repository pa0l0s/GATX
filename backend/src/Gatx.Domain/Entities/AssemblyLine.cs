using Gatx.Domain.Common;

namespace Gatx.Domain.Entities;

public sealed class AssemblyLine : Entity
{
    private readonly List<AssemblyLineWorkstation> _workstations = [];

    private AssemblyLine()
    {
        Name = string.Empty;
    }

    public AssemblyLine(Guid productId, string name, bool active)
    {
        ProductId = productId;
        Rename(name);
        Active = active;
    }

    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public IReadOnlyCollection<AssemblyLineWorkstation> Workstations => _workstations.AsReadOnly();

    public void Rename(string name)
    {
        var normalized = name.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Assembly line name is required.", nameof(name));
        }

        Name = normalized;
        MarkUpdated();
    }

    public void SetActive(bool active)
    {
        Active = active;
        MarkUpdated();
    }
}
