using Gatx.Domain.Common;

namespace Gatx.Domain.Entities;

public sealed class Workstation : Entity
{
    private readonly List<AssemblyLineWorkstation> _assemblyLines = [];

    private Workstation()
    {
        ShortName = string.Empty;
        Name = string.Empty;
        PcName = string.Empty;
    }

    public Workstation(string shortName, string name, string pcName)
    {
        Update(shortName, name, pcName);
    }

    public string ShortName { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PcName { get; private set; } = string.Empty;
    public IReadOnlyCollection<AssemblyLineWorkstation> AssemblyLines => _assemblyLines.AsReadOnly();

    public void Update(string shortName, string name, string pcName)
    {
        ShortName = NormalizeRequired(shortName, nameof(shortName));
        Name = NormalizeRequired(name, nameof(name));
        PcName = NormalizeRequired(pcName, nameof(pcName));
        MarkUpdated();
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return normalized;
    }
}
