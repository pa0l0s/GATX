using Gatx.Domain.Common;

namespace Gatx.Domain.Entities;

public sealed class Product : Entity
{
    private readonly List<AssemblyLine> _assemblyLines = [];

    private Product()
    {
        Name = string.Empty;
    }

    public Product(string name)
    {
        Rename(name);
    }

    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<AssemblyLine> AssemblyLines => _assemblyLines.AsReadOnly();

    public void Rename(string name)
    {
        var normalized = name.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (normalized.Length > 120)
        {
            throw new ArgumentException("Product name cannot exceed 120 characters.", nameof(name));
        }

        Name = normalized;
        MarkUpdated();
    }
}
